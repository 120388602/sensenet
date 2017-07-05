﻿using Lucene.Net.Documents;
using Lucene.Net.Search;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    internal abstract class LuceneQueryExecutor : IQueryExecutor
    {
        public PermissionChecker PermissionChecker { get; private set; }
        public LucQuery LucQuery { get; private set; }

        public void Initialize(LucQuery lucQuery, PermissionChecker permissionChecker)
        {
            this.LucQuery = lucQuery;
            this.PermissionChecker = permissionChecker;
        }

        public string QueryString
        {
            get { return this.LucQuery.ToString(); }
        }

        public int TotalCount { get; internal set; }


        public IEnumerable<LucObject> Execute()
        {
            using (var op = SnTrace.Query.StartOperation("LuceneQueryExecutor. CQL:{0}", this.LucQuery))
            {
                SearchResult result = null;

                int top = this.LucQuery.Top != 0 ? this.LucQuery.Top : this.LucQuery.PageSize;
                if (top == 0)
                    top = int.MaxValue;

                using (var readerFrame = LuceneManager.GetIndexReaderFrame(this.LucQuery.QueryExecutionMode == QueryExecutionMode.Quick))
                {
                    var idxReader = readerFrame.IndexReader;
                    var searcher = new IndexSearcher(idxReader);

                    var searchParams = new SearchParams
                    {
                        query = this.LucQuery.Query,
                        top = top,
                        executor = this,
                        searcher = searcher,
                        numDocs = idxReader.NumDocs()
                    };

                    try
                    {
                        result = DoExecute(searchParams);
                    }
                    finally
                    {
                        if (searchParams.searcher != null)
                        {
                            searchParams.searcher.Close();
                            searchParams.searcher = null;
                        }
                    }
                }

                this.TotalCount = result.totalCount;

                op.Successful = true;
                return result.result;
            }
        }

        protected internal bool IsPermitted(Document doc)
        {
            var nodeId = Convert.ToInt32(doc.Get(IndexFieldName.NodeId));
            var isLastPublic = BooleanIndexHandler.ConvertBack(doc.Get(IndexFieldName.IsLastPublic));
            var isLastDraft = BooleanIndexHandler.ConvertBack(doc.Get(IndexFieldName.IsLastDraft));

            return PermissionChecker.IsPermitted(nodeId, isLastPublic, isLastDraft);
        }

        protected SearchResult Search(SearchParams p)
        {
            var r = new SearchResult(null);

            var collector = CreateCollector(p.collectorSize, p);
            p.searcher.Search(p.query, collector);

            TopDocs topDocs = GetTopDocs(collector, p);
            r.totalCount = topDocs.TotalHits;
            var hits = topDocs.ScoreDocs;

            GetResultPage(hits, p, r);

            return r;
        }

        protected Collector CreateCollector(int size, SearchParams searchParams)
        {
            if (this.LucQuery.HasSort)
                return new SnTopFieldCollector(size, searchParams, new Sort(this.LucQuery.SortFields));
            return new SnTopScoreDocCollector(size, searchParams);
        }
        protected TopDocs GetTopDocs(Collector collector, SearchParams p)
        {
            return ((ISnCollector)collector).TopDocs(p.skip);
        }

        protected abstract void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r);

        protected abstract SearchResult DoExecute(SearchParams p);
    }
}

﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;
using SnQueryToStringVisitor = SenseNet.Search.Tests.Implementations.SnQueryToStringVisitor;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class CqlParserTests
    {
        [TestMethod]
        public void Search_Parser_AstToString_FromOriginalLuceneQueryParserSyntax()
        {
            Test("value", "_Text:value");
            Test("VALUE", "_Text:VALUE");
            Test("Value", "_Text:Value");
            Test("Value1", "_Text:Value1");
            Test("-Value1", "-_Text:Value1");
            Test("+Value1", "+_Text:Value1");
            Test("Value1 -Value2 +Value3 Value4", "_Text:Value1 -_Text:Value2 +_Text:Value3 _Text:Value4");
            Test("Field1:Value1");
            Test("#Field1:Value1");
            Test("-Field1:Value1");
            Test("+Field1:Value1");
            Test("Field1:Value1 Field2:Value2 Field3:Value3");
            Test("F1:V1 -F2:V2 +F3:V3 F4:V4");
            Test("f1:v1 f2:v2");
            Test("f1:v1 f2:v2 (f3:v3 f4:v4 (f5:v5 f6:v6))");
            Test("f1:v1 (f2:v2 (f3:v3 f4:v4))");
            Test("aaa AND +bbb", "+_Text:aaa +_Text:bbb");
            Test("te?t", "_Text:te?t");
            Test("test*", "_Text:test*");
            Test("te*t", "_Text:te*t");
            Test("roam~", "_Text:roam");
            Test("roam~" + SnQuery.DefaultFuzzyValue.ToString(CultureInfo.InvariantCulture), "_Text:roam");
            Test("roam~0.8", "_Text:roam~0.8");
            Test("\"jakarta apache\"~10", "_Text:'jakarta apache'~10");
            Test("mod_date:[20020101 TO 20030101]");
            Test("title:{Aida TO Carmen}");
            Test("jakarta apache", "_Text:jakarta _Text:apache");
            Test("jakarta^4 apache", "_Text:jakarta^4 _Text:apache");
            Test("\"jakarta apache\"^4 \"Apache Lucene\"", "_Text:'jakarta apache'^4 _Text:'Apache Lucene'");
            Test("\"jakarta apache\" jakarta", "_Text:'jakarta apache' _Text:jakarta");
            Test("\"jakarta apache\" OR jakarta", "_Text:'jakarta apache' _Text:jakarta");
            Test("\"jakarta apache\" AND \"Apache Lucene\"", "+_Text:'jakarta apache' +_Text:'Apache Lucene'");
            Test("+jakarta lucene", "+_Text:jakarta _Text:lucene");
            Test("\"jakarta apache\" NOT \"Apache Lucene\"", "_Text:'jakarta apache' -_Text:'Apache Lucene'");
            Test("NOT \"jakarta apache\"", "-_Text:'jakarta apache'");
            Test("\"jakarta apache\" -\"Apache Lucene\"", "_Text:'jakarta apache' -_Text:'Apache Lucene'");
            Test("(jakarta OR apache) AND website", "+(_Text:jakarta _Text:apache) +_Text:website");
            Test("title:(+return +\"pink panther\")", "+title:return +title:'pink panther'");
        }
        [TestMethod]
        public void Search_Parser_AstToString_PredicateTypes()
        {
            SnQuery q;
            q = Test("Name:aaa"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Id:1000"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Value:3.14"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
        }

        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_Ranges()
        {
            SnQuery q;
            q = Test("Name:<aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:>aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:<=aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:>=aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:<1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:>1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:<=1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:>=1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:<3.14");  Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:>3.14");  Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:<=3.14"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:>=3.14"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_SpecialChars()
        {
            Test("F1:V1 && F2:V2", "+F1:V1 +F2:V2");
            Test("F1:V1 || F2:V2", "F1:V1 F2:V2");
            Test("F1:V1 && F2:<>V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !F2:V2", "+F1:V1 -F2:V2");
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_Comments()
        {
            Test("F1:V1 //asdf", "F1:V1");
            Test("+F1:V1 /*asdf*/ +F2:V2 /*qwer*/", "+F1:V1 +F2:V2");
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlExtension_Controls()
        {
            // ".SELECT";
            // ".SKIP";
            // ".TOP";
            // ".SORT";
            // ".REVERSESORT";
            // ".AUTOFILTERS";
            // ".LIFESPAN";
            // ".COUNTONLY";
            // ".QUICK";

            var q = Test("F1:V1");
            Assert.AreEqual(int.MaxValue, q.Top);
            Assert.AreEqual(0, q.Skip);
            Assert.AreEqual(false, q.CountOnly);
            Assert.AreEqual(FilterStatus.Default, q.EnableAutofilters);
            Assert.AreEqual(FilterStatus.Default, q.EnableLifespanFilter);
            Assert.AreEqual(QueryExecutionMode.Default, q.QueryExecutionMode);
            Assert.AreEqual(null, q.Projection);
            Assert.AreEqual(0, q.Sort.Length);

            q = Test("F1:V1 .TOP:42", "F1:V1"); Assert.AreEqual(42, q.Top);
            q = Test("F1:V1 .SKIP:42", "F1:V1"); Assert.AreEqual(42, q.Skip);
            q = Test("F1:V1 .COUNTONLY", "F1:V1"); Assert.AreEqual(true, q.CountOnly);
            q = Test("F1:V1 .AUTOFILTERS:ON", "F1:V1"); Assert.AreEqual(FilterStatus.Enabled, q.EnableAutofilters);
            q = Test("F1:V1 .AUTOFILTERS:OFF", "F1:V1"); Assert.AreEqual(FilterStatus.Disabled, q.EnableAutofilters);
            q = Test("F1:V1 .LIFESPAN:ON", "F1:V1"); Assert.AreEqual(FilterStatus.Enabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .LIFESPAN:OFF", "F1:V1"); Assert.AreEqual(FilterStatus.Disabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .QUICK", "F1:V1"); Assert.AreEqual(QueryExecutionMode.Quick, q.QueryExecutionMode);
            q = Test("F1:V1 .SELECT:Name", "F1:V1"); Assert.AreEqual("Name", q.Projection);

            q = Test("F1:V1 .SORT:F1", "F1:V1"); Assert.AreEqual("F1 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .REVERSESORT:F1", "F1:V1"); Assert.AreEqual("F1 DESC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .SORT:F2", "F1:V1"); Assert.AreEqual("F1 ASC, F2 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .REVERSESORT:F3 .SORT:F2", "F1:V1"); Assert.AreEqual("F1 ASC, F3 DESC, F2 ASC", SortToString(q.Sort));

            TestError("F1:V1 .UNKNOWNKEYWORD", typeof(ParserException));
            TestError("F1:V1 .TOP", typeof(ParserException));
            TestError("F1:V1 .TOP:", typeof(ParserException));
            TestError("F1:V1 .TOP:aaa", typeof(ParserException));
            TestError("F1:V1 .SKIP", typeof(ParserException));
            TestError("F1:V1 .SKIP:", typeof(ParserException));
            TestError("F1:V1 .SKIP:aaa", typeof(ParserException));
            TestError("F1:V1 .COUNTONLY:", typeof(ParserException));
            TestError("F1:V1 .COUNTONLY:aaa", typeof(ParserException));
            TestError("F1:V1 .COUNTONLY:42", typeof(ParserException));
            TestError("F1:V1 .COUNTONLY:ON", typeof(ParserException));
            TestError("F1:V1 .AUTOFILTERS", typeof(ParserException));
            TestError("F1:V1 .AUTOFILTERS:", typeof(ParserException));
            TestError("F1:V1 .AUTOFILTERS:42", typeof(ParserException));
            TestError("F1:V1 .LIFESPAN", typeof(ParserException));
            TestError("F1:V1 .LIFESPAN:", typeof(ParserException));
            TestError("F1:V1 .LIFESPAN:42", typeof(ParserException));
            TestError("F1:V1 .QUICK:", typeof(ParserException));
            TestError("F1:V1 .QUICK:aaa", typeof(ParserException));
            TestError("F1:V1 .QUICK:42", typeof(ParserException));
            TestError("F1:V1 .QUICK:ON", typeof(ParserException));
            TestError("F1:V1 .SORT", typeof(ParserException));
            TestError("F1:V1 .SORT:", typeof(ParserException));
            TestError("F1:V1 .SORT:42", typeof(ParserException));
            TestError("F1:V1 .SELECT", typeof(ParserException));
            TestError("F1:V1 .SELECT:", typeof(ParserException));
            TestError("F1:V1 .SELECT:123", typeof(ParserException));
        }
        [TestMethod]
        public void Search_Parser_AstToString_CqlErrors()
        {
            TestError("", typeof(ParserException));
            TestError("()", typeof(ParserException));
            TestError("+(+(Id:1 Id:2) +Name:<b", typeof(ParserException));
            TestError("Id:(1 2 3", typeof(ParserException));
            TestError("Password:asdf", typeof(InvalidOperationException));
            TestError("PasswordHash:asdf", typeof(InvalidOperationException));
            TestError("Id::1", typeof(ParserException));
            TestError("Id:[10 to 15]", typeof(ParserException));
            TestError("Id:[10 TO 15", typeof(ParserException));
            TestError("Id:[ TO ]", typeof(ParserException));
            TestError("_Text:\"aaa bbb\"~", typeof(ParserException));
            TestError("Name:aaa~1.5", typeof(ParserException));
            TestError("Name:aaa^x", typeof(ParserException));
        }

        private SnQuery Test(string queryText, string expected = null)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();

            var snQuery = parser.Parse(queryText, queryContext);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(snQuery.QueryTree);
            var actualResult = visitor.Output;

            Assert.AreEqual(expected ?? queryText, actualResult);
            return snQuery;
        }
        private void TestError(string queryText, Type expectedExceptionType)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();
            Exception  thrownException = null;
            try
            {
                parser.Parse(queryText, queryContext);
            }
            catch (Exception e)
            {
                thrownException = e;
            }
            if (thrownException == null)
                Assert.Fail("Any exception wasn't thrown");
            if (thrownException.GetType() != expectedExceptionType)
                Assert.Fail($"{thrownException.GetType().Name} was thrown but {expectedExceptionType.Name} was expected.");
        }

        private string SortToString(SortInfo[] sortInfo)
        {
            return string.Join(", ", sortInfo.Select(s => $"{s.FieldName} {(s.Reverse ? "DESC" : "ASC")}").ToArray());
        }

        [TestMethod]
        public void Search_Parser_AggregateSettingsTest()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfoInt() }
            };
            // tuple values:
            // Item1: QuerySettings
            // Item2: query text postfix
            // Item3: expected Top
            // Item4: expected Skip
            // Item5: expected EnableAutofilters
            // Item6: expected EnableLifespanFilter
            // Item7: expected QueryExecutionMode
            var settings = new List<Tuple<QuerySettings, string, int, int, FilterStatus, FilterStatus, QueryExecutionMode>>
            {
                Tuple.Create(new QuerySettings(), " .TOP:0", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .TOP:5", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, "", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:0", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 0}, " .TOP:10", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 5}, " .TOP:10", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:5", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SKIP:0", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SKIP:1", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 0}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 0}, " .SKIP:1", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 1}, " .SKIP:0", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 10}, " .SKIP:5", int.MaxValue, 10, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 5}, " .SKIP:10", int.MaxValue, 5, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .AUTOFILTERS:ON", int.MaxValue, 0, FilterStatus.Enabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Enabled}, "", int.MaxValue, 0, FilterStatus.Enabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Disabled}, " .AUTOFILTERS:ON", int.MaxValue, 0, FilterStatus.Disabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .LIFESPAN:ON", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Enabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Enabled}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Enabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Disabled}, " .LIFESPAN:ON", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Disabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings() , " .QUICK", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Quick),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Quick}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Quick),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Strict}, " .QUICK", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Strict),
                Tuple.Create(new QuerySettings {Sort = new List<SortInfo> {new SortInfo ("Id") } }, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings (), " .SORT:Id", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Sort = new List<SortInfo> {new SortInfo("Id") } }, " .SORT:Name", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SORT:Name .TOP:0 .SORT:DisplayName", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default)
            };
            var expectedSortInfo = new List<IEnumerable<SortInfo>>();
            for (int i = 0; i < settings.Count - 4; i++)
            {
                expectedSortInfo.Add(null);
            }
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Name"), new SortInfo("DisplayName")});

            var parser = new CqlParser();
            var queryText = "+Id:<1000";
            foreach (var setting in settings)
            {
                var queryContext = new TestQueryContext(setting.Item1, 0, indexingInfo);
                var inputQueryText = queryText + setting.Item2;    
                var expectedResultText = queryText;

                var snQuery = parser.Parse(inputQueryText, queryContext);

                var visitor = new SnQueryToStringVisitor();
                visitor.Visit(snQuery.QueryTree);
                var actualResultText = visitor.Output;

                Assert.AreEqual(expectedResultText, actualResultText);
                Assert.AreEqual(setting.Item3, snQuery.Top);
                Assert.AreEqual(setting.Item4, snQuery.Skip);
                Assert.AreEqual(setting.Item5, snQuery.EnableAutofilters);
                Assert.AreEqual(setting.Item6, snQuery.EnableLifespanFilter);
                Assert.AreEqual(setting.Item7, snQuery.QueryExecutionMode);
                var sortIndex =  settings.IndexOf(setting);
                Assert.IsTrue((!snQuery.Sort.Any() && expectedSortInfo[sortIndex] == null) || expectedSortInfo[sortIndex].Count() == snQuery.Sort.Length);
            }
        }

        //UNDONE: Move this test to QueryClassifier tests
        //[TestMethod]
        //public void Search_Parser_UsedFieldNames()
        //{
        //    var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
        //    {
        //        {"Id", new TestPerfieldIndexingInfo_int() },
        //        {"Name", new TestPerfieldIndexingInfo_string() },
        //        {"Field1", new TestPerfieldIndexingInfo_string() },
        //        {"Field2", new TestPerfieldIndexingInfo_string() }
        //    };
        //    var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
        //    var parser = new CqlParser();
        //    var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2) +(Field1:asdf)";
        //    var expected = "Field1, Field2, Id, Name";

        //    var snQuery = parser.Parse(queryText, queryContext);

        //    var actual = string.Join(", ", snQuery.QueryFieldNames.OrderBy(x => x).ToArray());
        //    Assert.AreEqual(expected, actual);
        //}

    }
}

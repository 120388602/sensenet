﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;

namespace SenseNet.Search.Tests.Implementations
{
    public class TestIndexFieldHandlerString : IFieldIndexHandler
    {
        public bool Compile(QueryCompilerValue value)
        {
            value.Set(value.StringValue.ToLowerInvariant());
            return true;
        }

        public void ConvertToTermValue(QueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.String;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerInt : IFieldIndexHandler
    {
        public bool Compile(QueryCompilerValue value)
        {
            int converted;
            if (!int.TryParse(value.StringValue, out converted))
                return false;
            value.Set(converted);
            return true;
        }

        public void ConvertToTermValue(QueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Int;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerLong : IFieldIndexHandler
    {
        public bool Compile(QueryCompilerValue value)
        {
            long converted;
            if (!long.TryParse(value.StringValue, out converted))
                return false;
            value.Set(converted);
            return true;
        }

        public void ConvertToTermValue(QueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Long;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerSingle : IFieldIndexHandler
    {
        public bool Compile(QueryCompilerValue value)
        {
            float converted;
            if (!float.TryParse(value.StringValue, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out converted))
                return false;
            value.Set(converted);
            return true;
        }

        public void ConvertToTermValue(QueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Float;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
    public class TestIndexFieldHandlerDouble : IFieldIndexHandler
    {
        public bool Compile(QueryCompilerValue value)
        {
            double converted;
            if (!double.TryParse(value.StringValue, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out converted))
                return false;
            value.Set(converted);
            return true;
        }

        public void ConvertToTermValue(QueryFieldValue value)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAnalyzerName()
        {
            return typeof(KeywordAnalyzer).FullName;
        }

        public IEnumerable<string> GetParsableValues(ISnField field)
        {
            throw new NotImplementedException();
        }

        public int SortingType { get; }
        public IndexFieldType IndexFieldType { get; } = IndexFieldType.Double;
        public IPerFieldIndexingInfo OwnerIndexingInfo { get; set; }
        public string GetSortFieldName(string fieldName)
        {
            return fieldName;
        }

        public IEnumerable<IndexField> GetIndexFields(ISnField field, out string textExtract)
        {
            throw new NotImplementedException();
        }
    }
}

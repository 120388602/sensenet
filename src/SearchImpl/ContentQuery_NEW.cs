﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    //UNDONE: remove ReSharper comment if the ContentQuery_NEW is renamed well.
    // ReSharper disable once InconsistentNaming
    public class ContentQuery_NEW //UNDONE: Delete original ContentQuery and rename this to ContentQuery
    {
        private static readonly string[] QuerySettingParts = new[] { "SKIP", "TOP", "SORT", "REVERSESORT", "AUTOFILTERS", "LIFESPAN", "COUNTONLY" };
        private static readonly string RegexKeywordsAndComments = "//|/\\*|(\\.(?<keyword>[A-Z]+)(([ ]*:[ ]*[#]?\\w+(\\.\\w+)?)|([\\) $\\r\\n]+)))";
        private static readonly string RegexCommentEndSingle = "$";
        private static readonly string RegexCommentEndMulti = "\\*/|\\z";
        private static readonly string MultilineCommentStart = "/*";
        private static readonly string MultilineCommentEnd = "*/";

        private string _text;
        public string Text
        {
            get { return _text; }
            set { _text = FixMultilineComment(value); }
        }

        private QuerySettings _settings = new QuerySettings();
        public QuerySettings Settings
        {
            get { return _settings; }
            set { _settings = value ?? new QuerySettings(); }
        }

        public bool IsSafe { get; private set; }

        public static QueryResult Query(string text)
        {
            return Query(text, null, null);
        }
        public static QueryResult Query(string text, QuerySettings settings, params object[] parameters)
        {
            return CreateQuery(text, settings, parameters).Execute();
        }

        public static ContentQuery_NEW CreateQuery(string text)
        {
            return CreateQuery(text, null, null);
        }
        public static ContentQuery_NEW CreateQuery(string text, QuerySettings settings, params object[] parameters)
        {
            var isSafe = IsSafeQuery(text);
            if (parameters != null && parameters.Length > 0)
                text = SubstituteParameters(text, parameters);
            var query = new ContentQuery_NEW
            {
                Text = text,
                IsSafe = isSafe,
                Settings = settings,
            };
            return query;
        }
        private static bool IsSafeQuery(string queryText)
        {
            return SafeQueries.IsSafe(queryText);
        }
        private static string SubstituteParameters(string text, object[] parameters)
        {
            var stringValues = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                stringValues[i] = EscapeParameter(parameters[i]);

            var sb = new StringBuilder();
            var p = -1;
            while (++p < text.Length)
            {
                if (text[p] == '@')
                {
                    var q = p;
                    while (++q < text.Length && char.IsDigit(text[q])) { /* do nothing */ }
                    var nr = text.Substring(p + 1, q - p - 1);
                    if (nr.Length > 0)
                    {
                        var index = Int32.Parse(nr);
                        if (index >= parameters.Length)
                            throw new InvalidOperationException("Invalid format string.");
                        sb.Append(stringValues[index]);
                        p = q - 1;
                    }
                    else
                    {
                        sb.Append(text[p]);
                    }
                }
                else
                {
                    sb.Append(text[p]);
                }
            }
            return sb.ToString();
        }
        private static string EscapeParameter(object value)
        {
            var enumerableValue = value as IEnumerable;
            if (!(value is string) && enumerableValue != null)
            {
                var escaped = new List<string>();
                foreach (var x in enumerableValue)
                    if (x != null)
                        escaped.Add(EscapeParameter(x.ToString()));
                var joined = string.Join(" ", escaped);
                if (escaped.Count < 2)
                    return joined;
                return "(" + joined + ")";
            }
            else
            {
                var stringValue = value.ToString();
                var neeqQuot = false;
                foreach (var c in stringValue)
                {
                    if (Char.IsWhiteSpace(c))
                    {
                        neeqQuot = true;
                        break;
                    }
                    if (c == '\'' || c == '"' || c == '\\' || c == '+' || c == '-' || c == '&' || c == '|' || c == '!' || c == '(' || c == ')'
                         || c == '{' || c == '}' || c == '[' || c == ']' || c == '^' || c == '~' || c == '*' || c == '?' || c == ':' || c == '/' || c == '.')
                    {
                        neeqQuot = true;
                        break;
                    }
                }
                if (neeqQuot)
                    stringValue = "\"" + stringValue.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

                return stringValue;
            }
        }

        public void AddClause(string text)
        {
            AddClause(text, ChainOperator.And);
        }
        public void AddClause(string text, ChainOperator chainOp)
        {
            AddClause(text, chainOp, null);
        }
        public void AddClause(string text, ChainOperator chainOp, params object[] parameters)
        {
            var isSafe = this.IsSafe && IsSafeQuery(text);
            if (parameters != null && parameters.Length > 0)
                text = SubstituteParameters(text, parameters);
            this.IsSafe = isSafe;
            AddClausePrivate(text, chainOp);
        }
        private void AddClausePrivate(string text, ChainOperator chainOp)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (text.Length == 0)
                throw new ArgumentException("Clause cannot be empty", "text");

            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = text;
            }
            else
            {
                // we can modify the _text variable here directly because it was already fixed at init time
                switch (chainOp)
                {
                    case ChainOperator.And:
                        this._text = MoveSettingsToTheEnd(string.Format("+({0}) +({1})", Text, text)).Trim();
                        break;
                    case ChainOperator.Or:
                        this._text = MoveSettingsToTheEnd(string.Format("({0}) {1}", Text, text));
                        break;
                }
            }
        }
        /// <summary>
        /// This method moves all the settings keywords (e.g. SKIP, TOP, etc.) to the end of the text, skipping comments.
        /// </summary>
        /// <param name="queryText">Original query text</param>
        /// <returns>Updated query text</returns>
        private static string MoveSettingsToTheEnd(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
                return queryText;

            var backParts = string.Empty;
            var index = 0;
            var regex = new Regex(RegexKeywordsAndComments, RegexOptions.Multiline);

            while (true)
            {
                if (index >= queryText.Length)
                    break;

                // find the next setting keyword or comment start
                var match = regex.Match(queryText, index);
                if (!match.Success)
                    break;

                // if it is not a keyword than it is a comment --> skip it
                if (!match.Value.StartsWith("."))
                {
                    index = GetCommentEndIndex(queryText, match.Index);
                    continue;
                }

                // if we do not recognise the keyword, skip it (it may be in the middle of a text between quotation marks)
                if (!QuerySettingParts.Contains(match.Groups["keyword"].Value))
                {
                    index = match.Index + match.Length;
                    continue;
                }

                // remove the setting from the original position and store it
                queryText = queryText.Remove(match.Index, match.Length);
                index = match.Index;
                backParts += " " + match.Value;
            }

            // add the stored settings to the end of the query
            return string.Concat(queryText, backParts);
        }

        public static string AddClause(string originalText, string addition, ChainOperator chainOp)
        {
            if (addition == null)
                throw new ArgumentNullException("addition");
            if (addition.Length == 0)
                throw new ArgumentException("Clause cannot be empty", "addition");

            if (string.IsNullOrEmpty(originalText))
                return addition;

            var queryText = string.Empty;

            switch (chainOp)
            {
                case ChainOperator.And:
                    queryText = MoveSettingsToTheEnd(string.Format("+({0}) +({1})", originalText, addition)).Trim();
                    break;
                case ChainOperator.Or:
                    queryText = MoveSettingsToTheEnd(string.Format("({0}) {1}", originalText, addition));
                    break;
            }

            return queryText;
        }


        private static string FixMultilineComment(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
                return queryText;

            // find the last multiline comment
            var commentStartIndex = queryText.LastIndexOf(MultilineCommentStart, StringComparison.Ordinal);
            if (commentStartIndex < 0)
                return queryText;

            // find the end of the multiline comment: /* ... */
            var commentEndIndex = GetCommentEndIndex(queryText, commentStartIndex);
            if (commentEndIndex < queryText.Length - 1)
                return queryText;

            // comment is not closed --> close it manually
            return queryText + MultilineCommentEnd;
        }
        private static int GetCommentEndIndex(string queryText, int commentStartIndex)
        {
            // construct a single- or multiline end-commend regex
            var regexEndComment = new Regex(queryText.Substring(commentStartIndex, 2) == "//"
                ? RegexCommentEndSingle
                : RegexCommentEndMulti, RegexOptions.Multiline);

            var matchEndComment = regexEndComment.Match(queryText, commentStartIndex);

            // this will always be true, as both regexes contain the end-of-string character
            if (matchEndComment.Success)
            {
                return matchEndComment.Index + matchEndComment.Length;
            }

            return queryText.Length;
        }

        public QueryResult Execute()
        {
            var queryText = Text;

            if (string.IsNullOrEmpty(queryText))
                throw new InvalidOperationException("Cannot execute query with null or empty Text");

            var userId = AccessProvider.Current.GetCurrentUser().Id;
            if (userId == Identifiers.SystemUserId && !this.IsSafe)
            {
                var ex = new InvalidOperationException("Cannot execute this query, please convert it to a safe query.");
                ex.Data.Add("EventId", EventId.Querying);
                ex.Data.Add("Query", this._text);

                throw ex;
            }

            IEnumerable<int> identifiers;
            int totalCount;
            using (var op = SnTrace.Query.StartOperation("ContentQuery: {0} | Top:{1} Skip:{2} Sort:{3} Mode:{4}", queryText, _settings.Top, _settings.Skip, _settings.Sort, _settings.QueryExecutionMode))
            {
                if (!queryText.Contains("}}"))
                {
                    var result = SnQuery.Query(queryText, new SnQueryContext(Settings, userId));
                    identifiers = result.Hits;
                    totalCount = result.TotalCount;
                }
                else
                {
                    List<string> log;
                    identifiers = RecursiveExecutor.ExecuteRecursive(queryText, Settings.Top, Settings.Skip, Settings.Sort, Settings.EnableAutofilters,
                        Settings.EnableLifespanFilter, Settings.QueryExecutionMode,
                        userId, out totalCount, out log);
                }
                op.Successful = true;
            }
            return new QueryResult(identifiers, totalCount);
        }

        // ================================================================== Recursive executor class

        private static class RecursiveExecutor
        {
            private class InnerQueryResult
            {
                internal string[] StringArray;
                internal int[] IntArray;
            }

            private static readonly Regex EscaperRegex;
            static RecursiveExecutor()
            {
                var pattern = new StringBuilder("[");
                foreach (var c in SnLucLexer.STRINGTERMINATORCHARS.ToCharArray())
                    pattern.Append("\\" + c);
                pattern.Append("]");
                EscaperRegex = new Regex(pattern.ToString());
            }

            public static IEnumerable<int> ExecuteRecursive(string queryText, int top, int skip,
                IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode,
                int userId, out int count, out List<string> log)
            {
                log = new List<string>();
                IEnumerable<int> result;
                var src = queryText;
                log.Add(src);
                var control = GetControlString(src);

                while (true)
                {
                    int start;
                    var sss = GetInnerScript(src, control, out start);
                    var end = sss == string.Empty;

                    if (!end)
                    {
                        src = src.Remove(start, sss.Length);
                        control = control.Remove(start, sss.Length);

                        int innerCount;
                        var innerResult = ExecuteInnerScript(sss.Substring(2, sss.Length - 4), 0, 0,
                            sort, enableAutofilters, enableLifespanFilter, executionMode, true, userId, out innerCount).StringArray;

                        switch (innerResult.Length)
                        {
                            case 0:
                                sss = SnQuery.EmptyInnerQueryText;
                                break;
                            case 1:
                                sss = innerResult[0];
                                break;
                            default:
                                sss = string.Join(" ", innerResult);
                                sss = "(" + sss + ")";
                                break;
                        }
                        src = src.Insert(start, sss);
                        control = control.Insert(start, sss);
                        log.Add(src);
                    }
                    else
                    {
                        result = ExecuteInnerScript(src, top, skip, sort, enableAutofilters, enableLifespanFilter, executionMode,
                            false, userId, out count).IntArray;

                        log.Add(string.Join(" ", result.Select(i => i.ToString()).ToArray()));
                        break;
                    }
                }
                return result;
            }
            private static string GetControlString(string src)
            {
                var s = src.Replace("\\'", "__").Replace("\\\"", "__");
                var @out = new StringBuilder(s.Length);
                var instr = false;
                var strlimit = '\0';
                var esc = false;
                foreach (var c in s)
                {
                    if (c == '\\')
                    {
                        esc = true;
                        @out.Append('_');
                    }
                    else
                    {
                        if (esc)
                        {
                            esc = false;
                            @out.Append('_');
                        }
                        else
                        {
                            if (instr)
                            {
                                if (c == strlimit)
                                    instr = !instr;
                                @out.Append('_');
                            }
                            else
                            {
                                if (c == '\'' || c == '"')
                                {
                                    instr = !instr;
                                    strlimit = c;
                                    @out.Append('_');
                                }
                                else
                                {
                                    @out.Append(c);
                                }
                            }
                        }
                    }
                }

                var l0 = src.Length;
                var l1 = @out.Length;

                return @out.ToString();
            }
            private static string GetInnerScript(string src, string control, out int start)
            {
                start = 0;
                var p1 = control.IndexOf("}}");
                if (p1 < 0)
                    return string.Empty;
                var p0 = control.LastIndexOf("{{", p1);
                if (p0 < 0)
                    return string.Empty;
                start = p0;
                var ss = src.Substring(p0, p1 - p0 + 2);
                return ss;
            }
            private static InnerQueryResult ExecuteInnerScript(string queryText,
                int top, int skip, IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode,
                bool enableProjection, int userId, out int totalCount)
            {
                var querySettings = new QuerySettings
                {
                    Skip = skip,
                    Top = top,
                    Sort = sort,
                    EnableAutofilters = enableAutofilters,
                    EnableLifespanFilter = enableLifespanFilter,
                    QueryExecutionMode = executionMode
                };

                InnerQueryResult result;
                var queryContext = new SnQueryContext(querySettings, userId);
                if (enableProjection)
                {
                    var snQueryresult = SnQuery.QueryAndProject(queryText, queryContext);
                    result = new InnerQueryResult
                    {
                        StringArray = snQueryresult.Hits
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(EscapeForQuery)
                            .ToArray()
                    };
                    totalCount = snQueryresult.TotalCount;
                }
                else
                {
                    var snQueryresult = SnQuery.Query(queryText, queryContext);
                    result = new InnerQueryResult {IntArray = snQueryresult.Hits.ToArray()};
                    totalCount = snQueryresult.TotalCount;
                }

                return result;
            }
            private static string EscapeForQuery(string value)
            {
                if (EscaperRegex.IsMatch(value))
                    return string.Concat("'", value, "'");
                return value;
            }
        }
    }
}

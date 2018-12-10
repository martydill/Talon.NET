using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Talon
{
    public static class Quotations
    {
        private const int SplitterMaxLines = 4;

        private const decimal MaxLinesCount = 1000;

        private const string ReOnDateSmbWroteString = @"
        (
            -*  # could include dashes
            [ ]?On[ ].*,  # date part ends with comma
            (.*\n){0,3}  # splitter takes 4 lines at most
            .*(wrote|sent):
        )";

        private static readonly Regex ReOnDateSmbWrote = new Regex(ReOnDateSmbWroteString, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex[] SplitterPatterns =
        {
            // ------Original Message------ or ---- Reply Message ----
            new Regex(@"\A[\s]*[-]+[ ]*(Original|Reply) Message[ ]*[-]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // <date> <person>
            new Regex(@"\A(\d+/\d+/\d+|\d+\.\d+\.\d+).*@", RegexOptions.Compiled),
            new Regex(@"\A" + ReOnDateSmbWroteString, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace),
            new Regex(@"\A(_+\r?\n)?[\s]*(:?[*]?From|Date):[*]? .*", RegexOptions.Compiled),
            new Regex(@"\A\S{3,10}, \d\d? \S{3,10} 20\d\d,? \d\d?:\d\d(:\d\d)?" + 
                      @"\A( \S+){3,6}@\S+:", RegexOptions.Compiled)
        };

        private static readonly Regex ReQuotation = new Regex(@"
            (
                # quotation border: splitter line or a number of quotation marker lines
                (?:
                    s
                    |
                    (?:me*){2,}
                )

                # quotation lines could be marked as splitter or text, etc.
                .*

                # but we expect it to end with a quotation marker line
                me*
            )

            # after quotations should be text only or nothing at all
            [te]*$",
            RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex ReEmptyQuotation = new Regex(@"
            (
                # quotation border: splitter line or a number of quotation marker lines
                (?:
                    s
                    |
                    (?:me*){2,}
                )
            )
            e*",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex ReFwd = new Regex("^[-]+[ ]*Forwarded message[ ]*[-]+$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex ReQuotPattern = new Regex("^>+ ?", RegexOptions.Compiled);

        private static readonly Regex ReLink = new Regex("<(http://[^>]*)>", RegexOptions.Compiled);

        private static readonly Regex ReParenthesisLink_StartOfString = new Regex(@"\A\(https?://", RegexOptions.Compiled);

        private static readonly Regex ReParenthesisLink = new Regex(@"\(https?://", RegexOptions.Compiled);

        private static readonly Regex ReNormalizedLink = new Regex("@@(http://[^>@]*)@@", RegexOptions.Compiled);

        /// <summary>
        /// Extracts a non quoted message from provided plain text.
        /// </summary>
        public static string ExtractFromPlain(string msgBody)
        {
            var strippedText = msgBody;
            var delimiter = Utils.GetDelimiter(msgBody);
            msgBody = Preprocess(msgBody, delimiter);
            var lines = msgBody.SplitLines();

            // don't process too long messages
            if (lines.Length > MaxLinesCount)
                return strippedText;

            var markers = MarkMessageLines(lines);
            lines = ProcessMarkedLines(lines, markers).Lines;

            // concatenate lines, change links back, strip and return
            msgBody = String.Join(delimiter, lines);
            msgBody = PostProcess(msgBody);
            return msgBody;
        }

        public static string ExtractFrom(string msgBody, string contentType = "text/plain")
        {
            if (contentType == "text/plain")
                return ExtractFromPlain(msgBody);
            if (contentType == "text/html")
                return ExtractFromHtml(msgBody);

            return msgBody;
        }

        /// <summary>
        ///Extract not quoted message from provided html message body
        /// using tags and plain text algorithm.
        /// 
        /// Cut out the 'blockquote', 'gmail_quote' tags.
        /// Cut Microsoft quotations.
        /// 
        /// Then use plain text algorithm to cut out splitter or
        /// leftover quotation.
        /// This works by adding checkpoint text to all html tags,
        /// then converting html to text,
        /// then extracting quotations from text,
        /// then checking deleted checkpoints,
        /// then deleting necessary tags.
        /// </summary>
        public static string ExtractFromHtml(string msgBody)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepares msgBody for being stripped.
        /// Replaces link brackets so that they couldn't be taken for quotation marker.
        /// Splits line in two if splitter pattern preceded by some text on the same
        /// line (done only for 'On <date> <person> wrote:' pattern).
        /// </summary>
        internal static string Preprocess(string msgBody, string delimiter, string contentType = "text/plain")
        {
            // normalize links i.e. replace '<', '>' wrapping the link with some symbols
            // so that '>' closing the link couldn't be mistakenly taken for quotation
            // marker.

            msgBody = ReLink.Replace(msgBody, match =>
            {
                var newlineIndex = msgBody.Substring(0, match.Index).LastIndexOf("\n", StringComparison.Ordinal);
                if (msgBody[newlineIndex + 1] == '>')
                    return match.Groups[0].Value;
                else
                    return "@@" + match.Groups[1].Value + "@@";
            });

            if (contentType == "text/plain")
            {
                msgBody = ReOnDateSmbWrote.Replace(msgBody, splitter =>
                {
                    // Wraps splitter with new line
                    if (splitter.Index > 0 && msgBody[splitter.Index - 1] != '\n')
                        return delimiter + splitter.Groups[0].Value;
                    else
                        return splitter.Groups[0].Value;
                });
            }

            return msgBody;
        }

        /// <summary>
        /// Mark message lines with markers to distinguish quotation lines.
        ///
        /// Markers:
        ///
        /// e - empty line
        /// m - line that starts with quotation marker '>'
        /// s - splitter line
        /// t - presumably lines from the last message in the conversation
        /// f - forwarded message
        /// >>> MarkMessageLines(['answer', 'From: foo@bar.com', '', '> question'])
        /// 'tsem'
        /// </summary>
        internal static char[] MarkMessageLines(string[] lines)
        {
            var markers = new char[lines.Length];
            int i = 0;
            while (i < lines.Length)
            {
                if (String.IsNullOrWhiteSpace(lines[i]))
                {
                    markers[i] = 'e'; // Empty line
                }
                else if (ReQuotPattern.Match(lines[i]).Success)
                {
                    markers[i] = 'm'; // Line with quotation marker
                }
                else if (ReFwd.Match(lines[i]).Success)
                {
                    markers[i] = 'f'; // ---- Forwarded message ----
                }
                else
                {
                    var splitter = IsSplitter(String.Join("\n", lines.Skip(i).Take(SplitterMaxLines).ToArray()));
                    if (splitter != null)
                    {
                        // append as many splitter markers as lines in splitter
                        var splitterLines = splitter.Groups[0].Value.SplitLines();
                        for (int j = 0; j < splitterLines.Length; ++j)
                        {
                            markers[i + j] = 's';
                        }

                        // skip splitter lines
                        i += splitterLines.Length - 1;
                    }
                    else
                    {
                        // probably the line from the last message in the conversation
                        markers[i] = 't';
                    }
                }

                ++i;
            }

            return markers;
        }

        /// <summary>
        /// Returns Matcher object if provided string is a splitter and
        /// null otherwise.
        /// </summary>
        private static Match IsSplitter(string line)
        {
            return SplitterPatterns.Select(pattern => pattern.Match(line))
                .FirstOrDefault(match => match.Success);
        }

        // Holder for return value, since I don't really want to do 
        // Tuple<string[], Tuple<bool,int,int>> is getting a little excessive without constructor type inference...
        internal sealed class MarkedLinesResult
        {
            public string[] Lines { get; private set; }

            public Tuple<bool, int, int> ReturnFlags { get; private set; }

            public MarkedLinesResult(string[] lines, Tuple<bool,int,int> returnFlags)
            {
                Lines = lines;
                ReturnFlags = returnFlags;
            }
        }
        /// <summary>
        /// Run regexes against message's marked lines to strip quotations.
        /// Return only last message lines. 
        /// >>> process_marked_lines(['Hello', 'From: foo@bar.com', '', '> Hi', 'tsem'])
        /// ['Hello']
        ///
        /// Also returns returnFlags.
        /// returnFlags = [were_lines_deleted, first_deleted_line,
        ///                 last_deleted_line]
        /// </summary>
        internal static MarkedLinesResult ProcessMarkedLines(string[] lines, char[] markers, Tuple<bool, int, int> returnFlags = null)
        {
            if (returnFlags == null)
                returnFlags = new Tuple<bool, int, int>(false, -1, -1);

            string markersString = String.Join("", markers);

            // if there are no splitter there should be no markers
            if (!markers.Any(m => m == 's') && !Regex.Match(markersString, "(me*){3}").Success) // markers or markers[0]?
            {
                for (int i = 0; i < markers.Length; ++i)
                {
                    if (markers[i] == 'm')
                        markers[i] = 't';
                }
                markersString = String.Join("", markers);
            }

            if (Regex.Match(markersString, @"\A[te]*f").Success)
            {
                returnFlags = new Tuple<bool, int, int>(false, -1, -1);
                return new MarkedLinesResult(lines, returnFlags);
            }

            // inlined reply
            // use lookbehind assertions to find overlapping entries e.g. for 'mtmtm'
            // both 't' entries should be found
            var inlineMatches = Regex.Matches(markersString, "(?<=m)e*((?:t+e*)+)m");
            for (int i = 0; i < inlineMatches.Count; ++i)
            {
                var inlineReply = inlineMatches[i];

                // long links could break sequence of quotation lines but they shouldn't
                // be considered an inline reply
                var match = ReParenthesisLink.Match(lines[inlineReply.Index - 1]);
                if (!match.Success)
                    match = ReParenthesisLink_StartOfString.Match(lines[inlineReply.Index].Trim());

                if (!match.Success)
                {
                    returnFlags = new Tuple<bool, int, int>(false, -1, -1);
                    return new MarkedLinesResult(lines, returnFlags);
                }
            }

            // cut out text lines coming after splitter if there are no markers there
            var quotation = Regex.Match(markersString, "(se*)+((t|f)+e*)+");
            if (quotation.Success)
            {
                returnFlags = new Tuple<bool, int, int>(true, quotation.Index, lines.Length);
                return new MarkedLinesResult(lines.Take(quotation.Index).ToArray(), returnFlags);
            }

            // handle the case with markers
            quotation = ReQuotation.Match(markersString);
            if (!quotation.Success)
                quotation = ReEmptyQuotation.Match(markersString);

            if (quotation.Success)
            {
                returnFlags = new Tuple<bool, int, int>(true, quotation.Index, lines.Length);
                lines = lines.Take(quotation.Groups[1].Index)
                       .Concat(
                           lines.Skip(quotation.Groups[1].Length + quotation.Groups[1].Index))
                       .ToArray();

                return new MarkedLinesResult(lines, returnFlags);
            }

            returnFlags = new Tuple<bool, int, int>(false, -1, -1);
            return new MarkedLinesResult(lines, returnFlags);
        }

        /// <summary>
        /// Make up for changes done at preprocessing message.
        ///
        /// Replace link brackets back to '<' and '>'.
        /// </summary>
        private static string PostProcess(string msgBody)
        {
            return ReNormalizedLink.Replace(msgBody, @"<$1>").Trim();
        }
    }
}

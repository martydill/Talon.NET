using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Talon
{
    public static class Bruteforce
    {
        internal static int SignatureMaxLines = 11;
        internal static int TooLongSignatureLine = 60;

        // regex to fetch signature based on common signature words
        private static readonly Regex ReSignature = new Regex(@"
               (
                   (?:
                       ^[\s]*--*[\s]*[a-z \.^\r\n]*$
                       |
                       ^thanks[\s,!]*$
                       |
                       ^regards[\s,!]*$
                       |
                       ^cheers[\s,!]*$
                       |
                       ^best[ a-z]*[\s,!]*$
                   )
                   (\n|.)* # Extra \n to match Python's regex newline matching behavior
               )
            ",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Multiline);


        // signatures appended by phone email clients
        private static readonly Regex RePhoneSignature = new Regex(@"
               (
                   (?:
                       ^sent[ ]{1}from[ ]{1}my[\s,!\w]*$
                       |
                       ^sent[ ]from[ ]Mailbox[ ]for[ ]iPhone.*$
                       |
                       ^sent[ ]([\S]*[ ])?from[ ]my[ ]BlackBerry.*$
                       |
                       ^Enviado[ ]desde[ ]mi[ ]([\S]+[ ]){0,2}BlackBerry.*$
                   )
                   (\n|.)*
               )
               ",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Multiline);

        private static readonly Regex ReSignatureCandidate = new Regex(@"
            (?<candidate>c+d)[^d]
            |
            (?<candidate>c+d)$
            |
            (?<candidate>c+)
            |
            (?<candidate>d)[^d]
            |
            (?<candidate>d)$
            ",
             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace |
             RegexOptions.Multiline);

        /// <summary>
        /// Analyzes message for a presence of signature block (by common patterns)
        /// and returns tuple with two elements: message text without signature block
        /// and the signature itself.
        /// 
        /// >>> ExtractSignature('Hey man! How r u?\n\n--\nRegards,\nRoman')
        /// ('Hey man! How r u?', '--\nRegards,\nRoman')
        /// >>> ExtractSignature('Hey man!')
        /// ('Hey man!', None)
        /// </summary>
        public static Tuple<string, string> ExtractSignature(string msgBody)
        {
            // identify line delimiter first
            var delimiter = Utils.GetDelimiter(msgBody);

            // make an assumption
            var strippedBody = msgBody.Trim();

            // strip off phone signature
            string phoneSignatureString = null;
            var phoneSignature = RePhoneSignature.Match(msgBody);
            if (phoneSignature.Success)
            {
                strippedBody = String.Join("", strippedBody.Take(phoneSignature.Index));
                phoneSignatureString = phoneSignature.Value;
            }

            // decide on signature candidate
            var lines = strippedBody.SplitLines();
            var candidateLines = GetSignatureCandidate(lines);
            var candidate = String.Join(delimiter, candidateLines);

            // try to extract signature
            var signature = ReSignature.Match(candidate);
            if(!signature.Success)
            {
                return new Tuple<string, string>(strippedBody.Trim(), phoneSignatureString);
            }
            else
            {
                var signatureString = signature.Value;

                // when we splitlines() anthen join them
                // we can lose a new line at the end
                // we did it when identifying a candidate
                // so we had to do it for stripped_body now
                strippedBody = String.Join(delimiter, lines);
                strippedBody = strippedBody.Substring(0, strippedBody.Length - signatureString.Length);

                if (!String.IsNullOrWhiteSpace(phoneSignatureString))
                    signatureString = String.Join(delimiter, new[] {signatureString, phoneSignatureString});

                return new Tuple<string, string>(strippedBody.Trim(), signatureString.Trim());
            }
        }

       

        /// <summary>
        /// Return lines that could hold signature
        /// 
        /// The lines should:
        /// 
        /// * be among last SIGNATURE_MAX_LINES non-empty lines.
        /// * not include first line
        /// * be shorter than TOO_LONG_SIGNATURE_LINE
        /// * not include more than one line that starts with dashes
        /// </summary>
        internal static string[] GetSignatureCandidate(string[] lines)
        {
            // non empty lines indexes
            var nonEmptyLines =
                Enumerable.Range(0, lines.Length)
                    .Where(i => !String.IsNullOrWhiteSpace(lines[i]))
                    .Select(i => i)
                    .ToList();

            // if message is empty or just one line then there is no signature
            if (nonEmptyLines.Count() <= 1)
                return new string[0];

            //  we don't expect signature to start at the 1st line
            var candidate = nonEmptyLines.Skip(1).ToArray();

            // signature shouldn't be longer then SIGNATURE_MAX_LINES
            candidate = candidate.Skip(candidate.Count() - SignatureMaxLines).ToArray();

            var markers = MarkCandidateIndexes(lines, candidate);
            candidate = ProcessMarkedCandidateIndexes(candidate, markers);

            // get actual lines for the candidate instead of indexes
            if (candidate.Length > 0)
            {
                var linesToReturn = lines.Skip(candidate[0]).ToArray();
                return linesToReturn;
            }

            return new string[0];
        }

        /// <summary>
        /// Run regexes against candidate's marked indexes to strip
        /// signature candidate.
        ///
        /// >>> ProcessMarkedCandidateIndexes([9, 12, 14, 15, 17], 'clddc')
        /// [15, 17]
        /// </summary>
        internal static int[] ProcessMarkedCandidateIndexes(int[] candidate, char[] markers)
        {
            var match = ReSignatureCandidate.Match(String.Join("", markers.Reverse()));

            if (match.Success)
            {
                var group = match.Groups["candidate"];
                return candidate.Skip(candidate.Length - (group.Index + group.Length)).ToArray();
            }
            else
                return new int[0];
        }

        /// <summary>
        ///  """Mark candidate indexes with markers
        ///
        ///  Markers:
        ///  
        ///  * c - line that could be a signature line
        ///  * l - long line
        ///  * d - line that starts with dashes but has other chars as well
        ///   
        ///  >>> MarkCandidateIndexes(['Some text', '', '-', 'Bob'], [0, 2, 3])
        ///  'cdc'
        /// </summary>
        internal static char[] MarkCandidateIndexes(string[] lines, int[] candidate)
        {
            // at first consider everything to be potential signature lines
            var markers = Enumerable.Repeat('c', candidate.Length).ToArray();

            // mark lines starting from bottom up
            for(int i = markers.Length - 1; i >= 0; --i)
            {
                var candidateLine = lines[candidate[i]];
                if (candidateLine.Trim().Length > TooLongSignatureLine)
                {
                    markers[i] = 'l';
                }
                else
                {
                    var line = candidateLine.Trim();
                    if (line.StartsWith("-") && line.Replace("-", "").Length > 0)
                        markers[i] = 'd';
                }
            }

            return markers;
        }
    }
}

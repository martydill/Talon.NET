using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Talon
{
    internal static class Utils
    {
        internal static readonly Regex ReDelimiter = new Regex("\r?\n", RegexOptions.Compiled);
        
        internal static string GetDelimiter(string msgBody)
        {
            var match = ReDelimiter.Match(msgBody);
            if (match.Success)
                return match.Groups[0].Value;
            return "\n";
        }

        internal static string[] SplitLines(this string msg)
        {
            var lines = msg.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);

            // Removes the extra blank entry at the end to match the behaviour of Python's splitlines function
            if(lines.Any() && lines.Last() == "" && msg.EndsWith("\n"))
                return lines.Take(lines.Length - 1).ToArray();

            return lines;
        }
    }
}

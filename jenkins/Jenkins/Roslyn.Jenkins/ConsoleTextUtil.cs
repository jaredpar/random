using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    internal sealed class ConsoleTextUtil
    {
        private static readonly Regex s_csharpError = new Regex(@".*error CS\d+.*", RegexOptions.Compiled);
        private static readonly Regex s_basicError = new Regex(@".*error BC\d+.*", RegexOptions.Compiled);
        private static readonly Regex s_githubTimeout = new Regex(@"ERROR: Timeout after (\d)+ minutes", RegexOptions.Compiled);

        internal static bool TryGetFailureInfo(string consoleText, out JobFailureInfo failureInfo)
        {
            var lines = consoleText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return TryGetFailureInfo(lines, out failureInfo);
        }

        internal static bool TryGetFailureInfo(string[] consoleTextLines, out JobFailureInfo failureInfo)
        {
            JobFailureReason? reason = null;
            var list = new List<string>();

            foreach (var line in consoleTextLines)
            {
                var match = s_csharpError.Match(line);
                if (match.Success)
                {
                    reason = reason ?? JobFailureReason.Build;
                    list.Add(line);
                }

                match = s_basicError.Match(line);
                if (match.Success)
                {
                    reason = reason ?? JobFailureReason.Build;
                    list.Add(line);
                }

                match = s_githubTimeout.Match(line);
                if (match.Success)
                {
                    reason = JobFailureReason.Infrastructure;
                    list.Add(line);
                }
            }

            if (reason != null)
            {
                failureInfo = new JobFailureInfo(reason.Value, list);
                return true;
            }

            failureInfo = null;
            return false;
        }
    }
}

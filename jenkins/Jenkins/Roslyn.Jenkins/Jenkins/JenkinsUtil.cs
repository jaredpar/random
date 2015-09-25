using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public static class JenkinsUtil
    {
        private static readonly Dictionary<JobKind, string> s_kindToNameMap;
        private static readonly Dictionary<string, JobKind> s_nameToKindMap;

        public static readonly Uri JenkinsHost = new Uri("http://dotnet-ci.cloudapp.net");

        static JenkinsUtil()
        {
            s_kindToNameMap = new Dictionary<JobKind, string>();
            s_kindToNameMap[JobKind.WindowsDebug32] = "dotnet_roslyn_prtest_win_dbg_32";
            s_kindToNameMap[JobKind.WindowsDebug64] = "dotnet_roslyn_prtest_win_dbg_64";
            s_kindToNameMap[JobKind.WindowsRelease32] = "dotnet_roslyn_prtest_win_rel_32";
            s_kindToNameMap[JobKind.WindowsRelease64] = "dotnet_roslyn_prtest_win_rel_64";
            s_kindToNameMap[JobKind.WindowsDebugEta] = "dotnet_roslyn_prtest_win_dbg_eta";
            s_kindToNameMap[JobKind.Linux] = "dotnet_roslyn_prtest_lin";
            s_kindToNameMap[JobKind.Mac] = "dotnet_roslyn_prtest_mac";
            s_kindToNameMap[JobKind.LegacyWindows] = "dotnet_roslyn_prtest_win";

            s_nameToKindMap = new Dictionary<string, JobKind>(StringComparer.Ordinal);
            foreach (var pair in s_kindToNameMap)
            {
                s_nameToKindMap[pair.Value] = pair.Key;
            }
        }

        private static Uri GetUri(string path)
        {
            var builder = new UriBuilder(JenkinsHost);
            builder.Path = path;
            return builder.Uri;
        }

        public static IEnumerable<JobKind> GetAllJobKinds()
        {
            return Enum
                .GetValues(typeof(JobKind))
                .Cast<JobKind>();
        }

        public static string GetJobName(JobKind kind)
        {
            return s_kindToNameMap[kind];
        }

        public static JobKind GetJobKind(string jobName)
        {
            return s_nameToKindMap[jobName];
        }

        public static bool TryGetJobKind(string jobName, out JobKind kind)
        {
            return s_nameToKindMap.TryGetValue(jobName, out kind);
        }

        public static string GetJobPath(JobId id)
        {
            var name = GetJobName(id.Kind);
            return $"job/{name}/{id.Id}/";
        }

        public static Uri GetJobUri(JobId id)
        {
            return GetUri(GetJobPath(id));
        }

        public static string GetConsoleTextPath(JobId id)
        {
            return $"{GetJobPath(id)}consoleText";
        }

        public static Uri GetConsoleTextUri(JobId id)
        {
            return GetUri(GetConsoleTextPath(id));
        }
    }
}

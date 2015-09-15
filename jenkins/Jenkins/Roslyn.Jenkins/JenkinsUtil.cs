using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public static class JenkinsUtil
    {
        public static readonly Uri JenkinsHost = new Uri("http://dotnet-ci.cloudapp.net");

        private static Uri GetUri(string path)
        {
            var builder = new UriBuilder(JenkinsHost);
            builder.Path = path;
            return builder.Uri;
        }

        internal static string GetPlatformPathId(Platform platform)
        {
            switch (platform)
            {
                case Platform.Windows:
                    return "win";
                case Platform.Linux:
                    return "lin";
                case Platform.Mac:
                    return "mac";
                default:
                    throw new Exception($"Invalid platform: {platform}");
            }
        }

        public static string GetJobPath(JobId id)
        {
            var platform = GetPlatformPathId(id.Platform);
            return $"job/dotnet_roslyn_prtest_{platform}/{id.Id}/";
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

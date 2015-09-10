using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApiFun
{
    internal static class JenkinsUtil
    {
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

        internal static string GetUrlPath(JobId id)
        {
            var platform = GetPlatformPathId(id.Platform);
            return $"job/dotnet_roslyn_prtest_{platform}/{id.Id}/";
        }

        /// <summary>
        /// Is this a child build job.  If so return the ID of the parent job and base url
        /// </summary>
        internal static bool IsChildJob(JArray actions, out string baseUrl, out int parentBuildId)
        {
            baseUrl = null;
            parentBuildId = 0;

            var obj = actions.FirstOrDefault(x => x["causes"] != null);
            if (obj == null)
            {
                return false;
            }

            var array = (JArray)obj["causes"];
            if (array.Count == 0)
            {
                return false;
            }

            var data = array[0];
            baseUrl = data.Value<string>("upstreamUrl");
            parentBuildId = data.Value<int>("upstreamBuild");
            return baseUrl != null && parentBuildId != 0;
        }

        internal static PullRequestInfo ParseParentJobPullRequestInfo(JArray actions)
        {
            var container = actions.First(x => x["parameters"] != null);

            string sha1 = null;
            string pullLink = null;
            int? pullId = null;
            string authorEmail = null;

            foreach (var pair in (JArray)container["parameters"])
            {
                switch (pair.Value<string>("name"))
                {
                    case "ghprbActualCommit":
                        sha1 = pair.Value<string>("value");
                        break;
                    case "ghprbPullId":
                        pullId = pair.Value<int>("value");
                        break;
                    case "ghprbPullAuthorEmail":
                        authorEmail = pair.Value<string>("value");
                        break;
                    case "ghprbPullLink":
                        pullLink = pair.Value<string>("value");
                        break;
                    default:
                        break;
                }
            }

            if (sha1 == null || pullLink == null || pullId == null || authorEmail == null)
            {
                throw new Exception("Bad data");
            }

            return new PullRequestInfo(
                authorEmail: authorEmail,
                id: pullId.Value,
                pullUrl: pullLink,
                sha1: sha1);
        }
    }
}

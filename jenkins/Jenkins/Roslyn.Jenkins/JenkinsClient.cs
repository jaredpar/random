using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JenkinsClient
    {
        private readonly RestClient _restClient = new RestClient("http://dotnet-ci.cloudapp.net");

        public List<JobId> GetJobIds(Platform platform)
        {
            var platformId = JenkinsUtil.GetPlatformPathId(platform); 
            var data = GetJson($"job/dotnet_roslyn_prtest_{platformId}");
            var all = (JArray)data["builds"];

            var list = new List<JobId>();
            foreach (var cur in all)
            {
                var build = cur.ToObject<Json.Build>();
                list.Add(new JobId(build.Number, Platform.Windows));
            }

            return list;
        }

        public JobInfo GetJobInfo(JobId id)
        {
            var pr = GetPullRequestInfo(id);
            return new JobInfo(id, pr);
        }

        public PullRequestInfo GetPullRequestInfo(JobId id)
        {
            var path = JenkinsUtil.GetUrlPath(id);
            var data = GetJson(path);
            var actions = (JArray)data["actions"];

            string baseUrl;
            int parentBuildId;
            if (JenkinsUtil.IsChildJob(actions, out baseUrl, out parentBuildId))
            {
                return GetParentJobPullRequestInfo(baseUrl, parentBuildId);
            }

            // If it's not a child then it is the parent.
            return JenkinsUtil.ParseParentJobPullRequestInfo(actions);
        }

        private JObject GetJson(string urlPath)
        {
            urlPath = urlPath.TrimEnd('/');
            var request = new RestRequest($"{urlPath}/api/json", Method.GET);
            request.AddParameter("pretty", "true");
            var content = _restClient.Execute(request).Content;
            return JObject.Parse(content);
        }

        private PullRequestInfo GetParentJobPullRequestInfo(string baseUrl, int parentBuildId)
        {
            var data = GetJson($"{baseUrl}{parentBuildId}");
            var actions = (JArray)data["actions"];
            return JenkinsUtil.ParseParentJobPullRequestInfo(actions);
        }
    }
}

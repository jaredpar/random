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
    internal sealed class JenkinsClient
    {
        private readonly RestClient _restClient = new RestClient("http://dotnet-ci.cloudapp.net");

        internal List<Build> GetWindowsPullJobs()
        {
            var data = GetJson("job/dotnet_roslyn_prtest_win");
            var all = (JArray)data["builds"];

            var list = new List<Build>();
            foreach (var cur in all)
            {
                list.Add(cur.ToObject<Build>());
            }

            return list;
        }

        internal JobInfo GetJobInfo(Build build)
        {
            var pr = GetPullRequestInfo(build);
            return new JobInfo(build, pr);
        }

        private JObject GetJson(string urlPath)
        {
            urlPath = urlPath.TrimEnd('/');
            var request = new RestRequest($"{urlPath}/api/json", Method.GET);
            request.AddParameter("pretty", "true");
            var content = _restClient.Execute(request).Content;
            return JObject.Parse(content);
        }

        private PullRequestInfo GetPullRequestInfo(Build build)
        { 
            var uri = new Uri(build.Url);
            var data = GetJson(uri.PathAndQuery);
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

        private PullRequestInfo GetParentJobPullRequestInfo(string baseUrl, int parentBuildId)
        {
            var data = GetJson($"{baseUrl}{parentBuildId}");
            var actions = (JArray)data["actions"];
            return JenkinsUtil.ParseParentJobPullRequestInfo(actions);
        }
    }
}

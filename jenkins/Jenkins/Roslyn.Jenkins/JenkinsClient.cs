using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public JobResult GetJobResult(JobId id)
        {
            var path = JenkinsUtil.GetJobPath(id);
            var data = GetJson(path);

            bool? succeded = null;
            switch (data.Value<string>("result"))
            {
                case "SUCCESS":
                    succeded = true;
                    break;
                case "FAILURE":
                    succeded = false;
                    break;
            }

            if (succeded == null)
            {
                throw new Exception("Unable to determine the success / failure of the job");
            }

            if (!succeded.Value)
            {
                var failureInfo = GetJobFailureInfo(id, data);
                return new JobResult(id, failureInfo);
            }

            return new JobResult(id);
        }

        public PullRequestInfo GetPullRequestInfo(JobId id)
        {
            var path = JenkinsUtil.GetJobPath(id);
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

        /// <summary>
        /// Attempt to determine the failure reason for the given Job.  This should  only be called on 
        /// jobs that are known to have failed.
        /// </summary>
        private JobFailureInfo GetJobFailureInfo(JobId jobId, JObject data)
        {
            List<string> failedTestList;
            if (TryGetTestFailureReason(jobId, data, out failedTestList))
            {
                Debug.Assert(failedTestList.Count > 0);
                return JobFailureInfo.TestFailed(failedTestList);
            }

            return JobFailureInfo.Unknown;
        }

        private bool TryGetTestFailureReason(JobId jobId, JObject data, out List<string> failedTestList)
        {
            var actions = (JArray)data["actions"];
            foreach (var cur in actions)
            {
                var failCount = cur.Value<int?>("failCount");
                if (failCount != null)
                {
                    var testReportUrl = cur.Value<string>("urlName");
                    var path = $"{JenkinsUtil.GetJobPath(jobId)}{testReportUrl}/";
                    failedTestList = GetFailedTests(path);
                    return true;
                }
            }

            failedTestList = null;
            return false;
        }

        /// <summary>
        /// Get the list of failed test names from the specified test report URL
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> GetFailedTests(string testReportUrlPath)
        {
            var list = new List<string>();
            var data = GetJson(testReportUrlPath);
            var suites = (JArray)data["suites"];
            foreach (var suite in suites)
            {
                var cases = (JArray)suite["cases"];
                foreach (var cur in cases)
                {
                    var status = cur.Value<string>("status");
                    if (status == "PASSED" || status == "SKIPPED")
                    {
                        continue;
                    }

                    var className = cur.Value<string>("className");
                    var name = cur.Value<string>("name");
                    list.Add($"{className}.{name}");
                }
            }

            return list;
        }
    }
}

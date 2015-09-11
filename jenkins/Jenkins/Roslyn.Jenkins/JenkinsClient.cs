using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JenkinsClient
    {
        private static readonly Uri JenkinsHost = new Uri("http://dotnet-ci.cloudapp.net");

        private readonly RestClient _restClient = new RestClient(JenkinsHost.ToString());

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
            var path = JenkinsUtil.GetJobPath(id);
            var data = GetJson(path);
            var pr = GetPullRequestInfoCore(id, data);
            var uniqueJobId = GetUniqueJobId(id, data);
            return new JobInfo(uniqueJobId, pr);
        }

        public JobResult GetJobResult(JobId id)
        {
            var path = JenkinsUtil.GetJobPath(id);
            var data = GetJson(path);

            var result = data.Property("result");
            if (result == null)
            {
                throw new Exception("Could not find the result property");
            }

            JobState? state = null;
            switch (result.Value.Value<string>())
            {
                case "SUCCESS":
                    state = JobState.Succeeded;
                    break;
                case "FAILURE":
                    state = JobState.Failed;
                    break;
                case null:
                    state = JobState.Running;
                    break;
            }

            if (state == null)
            {
                throw new Exception("Unable to determine the success / failure of the job");
            }

            if (state.Value == JobState.Failed)
            {
                var failureInfo = GetJobFailureInfo(id, data);
                return new JobResult(id, failureInfo);
            }

            return new JobResult(id, state.Value);
        }

        public PullRequestInfo GetPullRequestInfo(JobId id)
        {
            var path = JenkinsUtil.GetJobPath(id);
            var data = GetJson(path);
            return GetPullRequestInfoCore(id, data);
        }

        private PullRequestInfo GetPullRequestInfoCore(JobId id, JObject data)
        {
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

        private UniqueJobId GetUniqueJobId(JobId id, JObject data)
        {
            var seconds = data.Value<long>("timestamp");
            var epoch = new DateTime(year: 1970, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);
            var date = epoch.AddMilliseconds(seconds);
            return new UniqueJobId(id, date);
        }

        public string GetConsoleText(JobId id)
        {
            var builder = new UriBuilder(JenkinsHost);
            builder.Path = $"{JenkinsUtil.GetJobPath(id)}consoleText";
            var request = WebRequest.Create(builder.Uri);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
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
            // First look for the test failure information.  
            List<string> failedTestList;
            if (TryGetTestFailureReason(jobId, data, out failedTestList))
            {
                Debug.Assert(failedTestList.Count > 0);
                return new JobFailureInfo(JobFailureReason.TestCase, failedTestList);
            }

            // Now look at the console text.
            var consoleText = GetConsoleText(jobId);
            JobFailureInfo failureInfo;
            if (ConsoleTextUtil.TryGetFailureInfo(consoleText, out failureInfo))
            {
                return failureInfo;
            }

            return JobFailureInfo.Unknown;
        }

        // TODO: This should be in JenkinsUtil
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

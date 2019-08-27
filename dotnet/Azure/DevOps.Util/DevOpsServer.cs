using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DevOps.Util
{
    public sealed class DevOpsServer
    {
        private string PersonalAccessToken { get; }
        public string Organization { get; }

        public DevOpsServer(string organization, string personalAccessToken = null)
        {
            Organization = organization;
            PersonalAccessToken = personalAccessToken;
        }

        /// <summary>
        /// List the builds that meet the provided query parameters
        /// </summary>
        /// <param name="buildNumber">Supports int based build numbers or * prefixes</param>
        public async Task<List<Build>> ListBuildsAsync(
            string project,
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTimeOffset? minTime = null,
            DateTimeOffset? maxTime = null,
            string requestedFor = null,
            BuildReason? reasonFilter = null,
            BuildStatus? statusFilter = null,
            BuildResult? resultFilter = null,
            int? top = null,
            int? maxBuildsPerDefinition = null,
            QueryDeletedOption? deletedFilter = null,
            BuildQueryOrder? queryOrder = null,
            string branchName = null,
            IEnumerable<int> buildIds = null,
            string repositoryId = null,
            string repositoryType = null)
        {
            var builder = GetBuilder(project, "build/builds");

            builder.AppendList("definitions", definitions);
            builder.AppendList("queues", queues);
            builder.AppendString("buildNumber", buildNumber);
            builder.AppendDateTime("minTime", minTime);
            builder.AppendDateTime("maxTime", maxTime);
            builder.AppendString("requestedFor", requestedFor);
            builder.AppendEnum("reasonFilter", reasonFilter);
            builder.AppendEnum("statusFilter", statusFilter);
            builder.AppendEnum("resultFilter", resultFilter);
            builder.AppendInt("$top", top);
            builder.AppendInt("maxBuildsPerDefinition", maxBuildsPerDefinition);
            builder.AppendEnum("deletedFilter", deletedFilter);
            builder.AppendEnum("queryOrder", queryOrder);
            builder.AppendString("branchName", branchName);
            builder.AppendList("buildIds", buildIds);
            builder.AppendString("repositoryId", repositoryId);
            builder.AppendString("repositoryType", repositoryType);
            return await ListItemsCore<Build>(builder, limit: top);
        }

        public async Task<Build> GetBuildAsync(string project, int buildId)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}");
            var json = await GetJsonResult(builder.ToString());
            return JsonConvert.DeserializeObject<Build>(json);
        }

        private string GetBuildLogsUri(string project, int buildId)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}/logs");
            return builder.ToString();
        }

        public async Task<BuildLog[]> GetBuildLogsAsync(string project, int buildId)
        {
            var uri = GetBuildLogsUri(project, buildId);
            var json = await GetJsonResult(uri);
            var root = JObject.Parse(json);
            var array = (JArray)root["value"];
            return array.ToObject<BuildLog[]>();
        }

        public async Task DownloadBuildLogsAsync(string project, int buildId, Stream stream)
        {
            var uri = GetBuildLogsUri(project, buildId);
            await DownloadZipFileAsync(uri, stream);
        }

        public async Task<MemoryStream> DownloadBuildLogsAsync(string project, int buildId) =>
            await WithMemoryStream(async s => await DownloadBuildLogsAsync(project, buildId));

        public async Task<string> GetBuildLogAsync(string project, int buildId, int logId, int? startLine = null, int? endLine = null)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}/logs/{logId}");
            builder.AppendInt("startLine", startLine);
            builder.AppendInt("endLine", endLine);

            using var client = CreateHttpClient();
            using (var response = await client.GetAsync(builder.ToString()))
            {
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }

        public async Task<Timeline> GetTimelineAsync(string project, int buildId)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}/timeline");
            var json = await GetJsonResult(builder.ToString());
            return JsonConvert.DeserializeObject<Timeline>(json);
        }

        public async Task<Timeline> GetTimelineAsync(Build build) => await GetTimelineAsync(build.Project.Name, build.Id);

        public async Task<Timeline> GetTimelineAsync(string project, int buildId, string timelineId, int? changeId = null)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}/timeline/{timelineId}");
            builder.AppendInt("changeId", changeId);
            var json = await GetJsonResult(builder.ToString());
            return JsonConvert.DeserializeObject<Timeline>(json);
        }

        public async Task<BuildArtifact[]> ListArtifactsAsync(string project, int buildId)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}/artifacts");
            var json = await GetJsonResult(builder.ToString());
            var root = JObject.Parse(json);
            var array = (JArray)root["value"];
            return array.ToObject<BuildArtifact[]>();
        }

        public async Task<BuildArtifact[]> ListArtifactsAsync(Build build) => await ListArtifactsAsync(build.Project.Name, build.Id);

        private string GetArtifactUri(string project, int buildId, string artifactName)
        {
            var builder = GetBuilder(project, $"build/builds/{buildId}/artifacts");
            builder.AppendString("artifactName", artifactName);
            return builder.ToString();
        }

        public async Task<BuildArtifact> GetArtifactAsync(string project, int buildId, string artifactName)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            var json = await GetJsonResult(uri);
            return JsonConvert.DeserializeObject<BuildArtifact>(json);
        }

        public async Task<MemoryStream> DownloadArtifactAsync(string project, int buildId, string artifactName) =>
            await WithMemoryStream(async s => await DownloadArtifactAsync(project, buildId, artifactName, s));

        public async Task DownloadArtifactAsync(string project, int buildId, string artifactName, Stream stream)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            await DownloadZipFileAsync(uri, stream);
        }

        public async Task<List<TeamProjectReference>> ListProjectsAsync(ProjectState? stateFilter = null, int? top = null, int? skip = null, bool? getDefaultTeamImageUrl = null)
        {
            var builder = GetBuilder(project: null, apiPath: "projects");
            builder.AppendEnum("stateFilter", stateFilter);
            builder.AppendInt("$top", top);
            builder.AppendInt("$skip", skip);
            builder.AppendBool("getDefaultTeamImageUrl", getDefaultTeamImageUrl);
            return await ListItemsCore<TeamProjectReference>(builder, limit: top);
        }

        private RequestBuilder GetBuilder(string project, string apiPath) => new RequestBuilder(Organization, project, apiPath);

        private async Task<string> GetJsonResult(string url) => (await GetJsonResultAndContinuationToken(url)).Body;

        private async Task<(string Body, string ContinuationToken)> GetJsonResultAndContinuationToken(string url)
        {
            using var client = CreateHttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string continuationToken = null;
            if (response.Headers.TryGetValues("x-ms-continuationtoken", out var values))
            {
                continuationToken = values.FirstOrDefault();
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            return (responseBody, continuationToken);
        }

        public async Task DownloadFileAsync(string uri, Stream destinationStream)
        {
            using var client = CreateHttpClient();
            using (var response = await client.GetAsync(uri))
            {
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(destinationStream);
            }
        }

        public async Task<MemoryStream> DownloadFileAsync(string uri) =>
            await WithMemoryStream(async s => await DownloadFileAsync(uri, s));

        public async Task DownloadZipFileAsync(string uri, Stream destinationStream)
        {
            using var client = CreateHttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/zip"));

            using (var response = await client.GetAsync(uri))
            {
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(destinationStream);
            }
        }

        public HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            AddAuthentication(client);
            return client;
        }

        public async Task<MemoryStream> DownloadZipFileAsync(string uri) =>
            await WithMemoryStream(async s => await DownloadFileAsync(uri, s));

        private void AddAuthentication(HttpClient client)
        {
            if (!string.IsNullOrEmpty(PersonalAccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{PersonalAccessToken}")));
            }
        }

        private async Task<MemoryStream> WithMemoryStream(Func<MemoryStream, Task> func)
        {
            var stream = new MemoryStream();
            await func(stream);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/builds/list?view=azure-devops-rest-5.0
        /// </summary>
        private async Task<List<T>> ListItemsCore<T>(
            RequestBuilder builder, 
            int? limit = null)
        {
            var list = new List<T>();
            await ListItemsCore<T>(
                items =>
                {
                    list.AddRange(items);
                    return default;
                }, builder, limit);
            return list;
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/builds/list?view=azure-devops-rest-5.0
        /// </summary>
        private async Task ListItemsCore<T>(
            Func<T[], ValueTask> processItems,
            RequestBuilder builder, 
            int? limit = null)
        {
            Debug.Assert(string.IsNullOrEmpty(builder.ContinuationToken));
            var count = 0;
            do
            {
                var (json, token) = await GetJsonResultAndContinuationToken(builder.ToString());
                var root = JObject.Parse(json);
                var array = (JArray)root["value"];
                var items = array.ToObject<T[]>();
                await processItems(items);

                count += items.Length;
                if (token is null)
                {
                    break;
                }

                if (limit.HasValue && count > limit.Value)
                {
                    break;
                }

                builder.ContinuationToken = token;
            } while (true);
        }
    }
}

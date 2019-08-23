using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/builds/list?view=azure-devops-rest-5.0
        /// </summary>
        private async Task<(Build[] Builds, string ContinuationToken)> ListBuildsCoreAsync(
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
            string repositoryType = null,
            string continuationToken = null)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append("/build/builds?");

            appendList(builder, "definitions", definitions);
            appendList(builder, "queues", queues);
            appendString(builder, "buildNumber", buildNumber);
            appendDateTime(builder, "minTime", minTime);
            appendDateTime(builder, "maxTime", maxTime);
            appendString(builder, "requestedFor", requestedFor);
            appendEnum(builder, "reasonFilter", reasonFilter);
            appendEnum(builder, "statusFilter", statusFilter);
            appendEnum(builder, "resultFilter", resultFilter);
            appendInt(builder, "$top", top);
            appendString(builder, "continuationToken", continuationToken);
            appendInt(builder, "maxBuildsPerDefinition", maxBuildsPerDefinition);
            appendEnum(builder, "deletedFilter", deletedFilter);
            appendEnum(builder, "queryOrder", queryOrder);
            appendString(builder, "branchName", branchName);
            appendList(builder, "buildIds", buildIds);
            appendString(builder, "repositoryId", repositoryId);
            appendString(builder, "repositoryType", repositoryType);

            builder.Append("api-version=5.0");
            var (json, token) = await GetJsonResultAndContinuationToken(builder.ToString());
            var root = JObject.Parse(json);
            var array = (JArray)root["value"];
            return (array.ToObject<Build[]>(), token);

            static void appendList(StringBuilder builder, string name, IEnumerable<int> values)
            {
                if (values is null || !values.Any())
                {
                    return;
                }

                builder.Append($"{name}=");
                var first = true;
                foreach (var value in values)
                {
                    if (!first)
                    {
                        builder.Append(",");
                    }
                    builder.Append(value);
                    first = false;
                }
                builder.Append("&");
            }

            static void appendString(StringBuilder builder, string name, string value, bool escape = true)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (escape)
                    {
                        value = Uri.EscapeDataString(value);
                    }

                    builder.Append($"{name}={value}");
                }
            }

            static void appendInt(StringBuilder builder, string name, int? value)
            {
                if (value.HasValue)
                {
                    builder.Append($"{name}={value.Value}");
                }
            }

            static void appendDateTime(StringBuilder builder, string name, DateTimeOffset? value)
            {
                if (value.HasValue)
                {
                    builder.Append($"{name}=");
                    builder.Append(value.Value.UtcDateTime.ToString("o"));
                }
            }

            static void appendEnum<T>(StringBuilder builder, string name, T? value) where T: struct, Enum
            {
                if (value.HasValue)
                {
                    var lowerValue = value.Value.ToString();
                    lowerValue = char.ToLower(lowerValue[0]) + lowerValue.Substring(1);
                    builder.Append($"{name}={lowerValue}&");
                }
            }
        }

        /// <summary>
        /// List the builds that meet the provided query parameters
        /// </summary>
        /// <param name="buildNumber">Supports int based build numbers or * prefixes</param>
        public async Task ListBuildsAsync(
            Func<Build[], Task> processBuilds,
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
            string continuationToken = null;
            var count = 0;
            do
            {
                var tuple = await ListBuildsCoreAsync(
                    project,
                    definitions,
                    queues,
                    buildNumber,
                    minTime,
                    maxTime,
                    requestedFor,
                    reasonFilter,
                    statusFilter,
                    resultFilter,
                    top,
                    maxBuildsPerDefinition,
                    deletedFilter,
                    queryOrder,
                    branchName,
                    buildIds,
                    repositoryId,
                    repositoryType,
                    continuationToken);
                await processBuilds(tuple.Builds);
                continuationToken = tuple.ContinuationToken;
                count += tuple.Builds.Length;

                if (continuationToken is null)
                {
                    break;
                }

                if (top.HasValue && count > top.Value)
                {
                    break;
                }

            } while (true);
        }

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
            var builds = new List<Build>();
            await ListBuildsAsync(
                processBuilds,
                project,
                definitions,
                queues,
                buildNumber,
                minTime,
                maxTime,
                requestedFor,
                reasonFilter,
                statusFilter,
                resultFilter,
                top,
                maxBuildsPerDefinition,
                deletedFilter,
                queryOrder,
                branchName,
                buildIds,
                repositoryId,
                repositoryType);

            return builds;

            Task processBuilds(Build[] b)
            {
                builds.AddRange(b);
                return Task.CompletedTask;
            }
        }

        public async Task<Build> GetBuildAsync(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}?api-version=5.0");
            var json = await GetJsonResult(builder.ToString());
            return JsonConvert.DeserializeObject<Build>(json);
        }

        private string GetBuildLogsUri(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/logs?api-version=5.0");
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
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/logs/{logId}?");

            var first = true;
            if (startLine.HasValue)
            {
                builder.Append($"startLine={startLine}");
                first = false;
            }

            if (endLine.HasValue)
            {
                if (!first)
                {
                    builder.Append("&");
                }

                builder.Append($"endLine={endLine}");
                first = false;
            }

            if (!first)
            {
                builder.Append("&");
            }

            builder.Append("api-version=5.0");
            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(builder.ToString()))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
        }

        public async Task<Timeline> GetTimelineAsync(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/timeline?api-version=5.0");
            var json = await GetJsonResult(builder.ToString());
            return JsonConvert.DeserializeObject<Timeline>(json);
        }

        public async Task<Timeline> GetTimelineAsync(Build build) => await GetTimelineAsync(build.Project.Name, build.Id);

        public async Task<Timeline> GetTimelineAsync(string project, int buildId, string timelineId, int? changeId = null)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/timeline/{timelineId}?");

            if (changeId.HasValue)
            {
                builder.Append($"changeId={changeId}&");
            }

            var json = await GetJsonResult(builder.ToString());
            return JsonConvert.DeserializeObject<Timeline>(json);
        }

        public async Task<BuildArtifact[]> ListArtifactsAsync(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/artifacts?api-version=5.0");
            var json = await GetJsonResult(builder.ToString());
            var root = JObject.Parse(json);
            var array = (JArray)root["value"];
            return array.ToObject<BuildArtifact[]>();
        }

        public async Task<BuildArtifact[]> ListArtifactsAsync(Build build) => await ListArtifactsAsync(build.Project.Name, build.Id);

        private string GetArtifactUri(string project, int buildId, string artifactName)
        {
            var builder = GetProjectApiRootBuilder(project);
            artifactName = Uri.EscapeDataString(artifactName);
            builder.Append($"/build/builds/{buildId}/artifacts?artifactName={artifactName}&api-version=5.0");
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

        private StringBuilder GetProjectApiRootBuilder(string project)
        {
            var builder = new StringBuilder();
            builder.Append($"https://dev.azure.com/{Organization}/{project}/_apis");
            return builder;
        }

        private async Task<string> GetJsonResult(string url) => (await GetJsonResultAndContinuationToken(url)).Body;

        private async Task<(string Body, string ContinuationToken)> GetJsonResultAndContinuationToken(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                AddAuthentication(client);

                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    string continuationToken = null;
                    if (response.Headers.TryGetValues("x-ms-continuationtoken", out var values))
                    {
                        continuationToken = values.FirstOrDefault();
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    return (responseBody, continuationToken);
                }
            }
        }

        public async Task DownloadFileAsync(string uri, Stream destinationStream)
        {
            using (var client = new HttpClient())
            {
                AddAuthentication(client);

                using (var response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    await response.Content.CopyToAsync(destinationStream);
                }
            }
        }

        public async Task<MemoryStream> DownloadFileAsync(string uri) =>
            await WithMemoryStream(async s => await DownloadFileAsync(uri, s));

        public async Task DownloadZipFileAsync(string uri, Stream destinationStream)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/zip"));

                AddAuthentication(client);

                using (var response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    await response.Content.CopyToAsync(destinationStream);
                }
            }
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
    }
}

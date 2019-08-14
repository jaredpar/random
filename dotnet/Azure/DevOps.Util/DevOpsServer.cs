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
        public async Task<Build[]> ListBuildsAsync(string project, IEnumerable<int> definitions = null, int? top = null)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append("/build/builds?");

            if (definitions?.Any() == true)
            {
                builder.Append("definitions=");
                var first = true;
                foreach (var definition in definitions)
                {
                    if (!first)
                    {
                        builder.Append(",");
                    }
                    builder.Append(definition);
                    first = false;
                }
                builder.Append("&");
            }

            if (top.HasValue)
            {
                builder.Append($"$top={top.Value}&");
            }

            builder.Append("api-version=5.0");
            var json = await GetJsonResult(builder.ToString());
            var root = JObject.Parse(json);
            var array = (JArray)root["value"];
            return array.ToObject<Build[]>();
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

        public async Task DownloadBuildLogsAsync(string project, int buildId, string filePath)
        {
            var uri = GetBuildLogsUri(project, buildId);
            await DownloadFile(uri, filePath);
        }

        public async Task DownloadBuildLogsAsync(string project, int buildId, Stream stream)
        {
            var uri = GetBuildLogsUri(project, buildId);
            await DownloadZipFile(uri, stream);
        }

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

        public async Task DownloadArtifactAsync(string project, int buildId, string artifactName, string filePath)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            await DownloadFile(uri, filePath);
        }

        public async Task DownloadArtifactAsync(string project, int buildId, string artifactName, Stream stream)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            await DownloadZipFile(uri, stream);
        }

        private StringBuilder GetProjectApiRootBuilder(string project)
        {
            var builder = new StringBuilder();
            builder.Append($"https://dev.azure.com/{Organization}/{project}/_apis");
            return builder;
        }

        private async Task<string> GetJsonResult(string uri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                AddAuthentication(client);

                using (var response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
        }

        public async Task DownloadFile(string uri, Stream destinationStream)
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

        public async Task DownloadZipFile(string uri, Stream destinationStream)
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

        private async Task DownloadFile(string uri, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                await DownloadZipFile(uri, fileStream);
            }
        }

        private void AddAuthentication(HttpClient client)
        {
            if (!string.IsNullOrEmpty(PersonalAccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{PersonalAccessToken}")));
            }
        }
    }
}

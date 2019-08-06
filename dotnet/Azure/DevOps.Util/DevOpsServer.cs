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
        public async Task<string> ListBuildRaw(string project, IEnumerable<int> definitions = null, int? top = null)
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
            return await GetJsonResult(builder.ToString());
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/builds/list?view=azure-devops-rest-5.0
        /// </summary>
        public async Task<Build[]> ListBuild(string project, IEnumerable<int> definitions = null, int? top = null)
        {
            var root = JObject.Parse(await ListBuildRaw(project, definitions, top));
            var array = (JArray)root["value"];
            return array.ToObject<Build[]>();
        }

        public async Task<string> GetBuildLogsRaw(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/logs?api-version=5.0");
            return await GetJsonResult(builder.ToString());
        }

        public async Task<BuildLog[]> GetBuildLogs(string project, int buildId)
        {
            var root = JObject.Parse(await GetBuildLogsRaw(project, buildId));
            var array = (JArray)root["value"];
            return array.ToObject<BuildLog[]>();
        }

        public async Task<string> GetBuildLog(string project, int buildId, int logId, int? startLine = null, int? endLine = null)
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

        public async Task<string> GetTimelineRaw(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/timeline?api-version=5.0");
            return await GetJsonResult(builder.ToString());
        }

        public async Task<string> GetTimelineRaw(string project, int buildId, string timelineId, int? changeId = null)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/timeline/{timelineId}?");

            if (changeId.HasValue)
            {
                builder.Append($"changeId={changeId}&");
            }

            return await GetJsonResult(builder.ToString());
        }

        public async Task<Timeline> GetTimeline(string project, int buildId)
        {
            var json = await GetTimelineRaw(project, buildId);
            return JsonConvert.DeserializeObject<Timeline>(json);
        }

        public async Task<Timeline> GetTimeline(string project, int buildId, string timelineId, int? changeId = null)
        {
            var json = await GetTimelineRaw(project, buildId, timelineId, changeId);
            return JsonConvert.DeserializeObject<Timeline>(json);
        }

        public async Task<string> ListArtifactsRaw(string project, int buildId)
        {
            var builder = GetProjectApiRootBuilder(project);
            builder.Append($"/build/builds/{buildId}/artifacts?api-version=5.0");
            return await GetJsonResult(builder.ToString());
        }

        public async Task<BuildArtifact[]> ListArtifacts(string project, int buildId)
        {
            var root = JObject.Parse(await ListArtifactsRaw(project, buildId));
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

        public async Task<string> GetArtifactRaw(string project, int buildId, string artifactName)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            return await GetJsonResult(uri);
        }

        public async Task<BuildArtifact> GetArtifact(string project, int buildId, string artifactName)
        {
            var json = await GetArtifactRaw(project, buildId, artifactName);
            return JsonConvert.DeserializeObject<BuildArtifact>(json);
        }

        public async Task DownloadArtifact(string project, int buildId, string artifactName, string filePath)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            await DownloadFile(uri, filePath);
        }

        public async Task DownloadArtifact(string project, int buildId, string artifactName, Stream stream)
        {
            var uri = GetArtifactUri(project, buildId, artifactName);
            await GetFileResult(uri, stream);
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

        private async Task GetFileResult(string uri, Stream destinationStream)
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
                await GetFileResult(uri, fileStream);
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

    /*
	try
	{
		var personalaccesstoken = "PAT_FROM_WEBSITE";

		using (HttpClient client = new HttpClient())
		{
			client.DefaultRequestHeaders.Accept.Add(
				new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
				Convert.ToBase64String(
					System.Text.ASCIIEncoding.ASCII.GetBytes(
						string.Format("{0}:{1}", "", personalaccesstoken))));

			using (HttpResponseMessage response = await client.GetAsync(
						"https://dev.azure.com/{organization}/_apis/projects"))
			{
				response.EnsureSuccessStatusCode();
				string responseBody = await response.Content.ReadAsStringAsync();
				Console.WriteLine(responseBody);
			}
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.ToString());
	}
    */
}

﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Query.Util
{
    public sealed class AzureServer
    {
        public string Organization { get; }

        public AzureServer(string organization)
        {
            Organization = organization;
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
        public async Task<BuildData[]> ListBuild(string project, IEnumerable<int> definitions = null, int? top = null)
        {
            var root = JObject.Parse(await ListBuildRaw(project, definitions, top));
            var array = (JArray)root["value"];
            return array.ToObject<BuildData[]>();
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

            builder.Append($"api-version=5.0");
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

                using (var response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
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

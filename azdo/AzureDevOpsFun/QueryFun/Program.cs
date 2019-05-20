using Query.Util;
using System;
using System.Threading.Tasks;
using System.IO;

namespace QueryFun
{
    public class Program
    {
        public static string Organization = "dnceng";

        public static async Task Main(string[] args)
        {
            await DumpBuild("public", 194440);
        }

        private static async Task DumpBuild(string project, int buildId)
        {
            var server = new AzureServer(Organization);
            var output = @"e:\temp\logs";
            Directory.CreateDirectory(output);
            foreach (var log in await server.GetBuildLogs(project, buildId))
            {
                var logFilePath = Path.Combine(output, $"{log.Id}.txt");
                Console.WriteLine($"Log Id {log.Id} {log.Type} - {logFilePath}");
                var content = await server.GetBuildLog(project, buildId, log.Id);
                File.WriteAllText(logFilePath, content);

            }
        }

        private static async Task Fun()
        { 
            var server = new AzureServer(Organization);
            var project = "public";
            foreach (var build in await server.ListBuild(project, definitions: new[] { 15 }, top: 10))
            {
                Console.WriteLine($"{build.Id} {build.BuildNumber} {build.BuildUri}");
                var buildLogs = await server.GetBuildLogs(project, build.Id);
                Console.WriteLine($"{buildLogs.Length} logs");
            }
        }
    }
}

/*
 * public static async void GetProjects()
{
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
}
*/

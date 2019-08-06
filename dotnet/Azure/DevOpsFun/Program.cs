using DevOps.Util;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO.Compression;

namespace QueryFun
{
    public class Program
    {
        public static string Organization = "dnceng";

        public static async Task Main(string[] args)
        {
            await DumpNgenLog(2916584, @"p:\temp\ngen");
            // await Fun();
            // await DumpTimeline("public", 196140);
        }

        private static async Task<string> GetToken()
        {
            var text = await File.ReadAllLinesAsync(@"p:\tokens.txt");
            return text[0].Split(':')[1];
        }

        private static async Task DumpNgenLog(int buildId, string targetDirPath)
        {
            var targetDir = Directory.CreateDirectory(targetDirPath);
            foreach (var fileInfo in targetDir.GetFiles())
            {
                File.Delete(fileInfo.FullName);
            }

            var server = new DevOpsServer("devdiv", await GetToken());
            var project = "DevDiv";
            var stream = new MemoryStream();
            await server.DownloadArtifact(project, buildId, "Build Diagnostic Files", stream);
            stream.Position = 0;
            using (var zipArchive = new ZipArchive(stream))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    if (entry.FullName.StartsWith("Build Diagnostic Files/ngen/"))
                    {
                        using var entryStream = entry.Open();
                        entry.Name


                    }
                }

            }
        }

        private static async Task DumpNgenLogFun()
        {
            var server = new DevOpsServer("devdiv", await GetToken());
            string project = "devdiv";
            var buildId = 2916584;
            var all = await server.ListArtifacts(project, buildId);
            var buildArtifact = await server.GetArtifact(project, buildId, "Build Diagnostic Files");
            var filePath = @"p:\temp\data.zip";
            await server.DownloadArtifact(project, buildId, "Build Diagnostic Files", filePath);
        }

        private static async Task DumpTimeline(string project, int buildId)
        {
            var server = new DevOpsServer(Organization);

            var timeline = await server.GetTimeline(project, buildId);
            await DumpTimeline("", timeline);
            async Task DumpTimeline(string indent, Timeline timeline)
            {
                foreach (var record in timeline.Records)
                {
                    Console.WriteLine($"{indent}Record {record.Name}");
                    if (record.Issues != null)
                    {
                        foreach (var issue in record.Issues)
                        {
                            Console.WriteLine($"{indent}{issue.Type} {issue.Category} {issue.Message}");
                        }
                    }

                    if (record.Details is object)
                    {
                        var nextIndent = indent + "\t";
                        var subTimeline = await server.GetTimeline(project, buildId, record.Details.Id, record.Details.ChangeId);
                        await DumpTimeline(nextIndent, subTimeline);
                    }
                }
            }
        }

        private static async Task DumpBuild(string project, int buildId)
        {
            var server = new DevOpsServer(Organization);
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
            var server = new DevOpsServer(Organization);
            var project = "public";
            var builds = await server.ListBuild(project, definitions: new[] { 15 }, top: 10);
            foreach (var build in builds)
            {
                Console.WriteLine($"{build.Id} {build.Uri}");
                var artifacts = await server.ListArtifacts(project, build.Id);
                foreach (var artifact in artifacts)
                {
                    Console.WriteLine($"\t{artifact.Id} {artifact.Name} {artifact.Resource.Type}");
                }
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

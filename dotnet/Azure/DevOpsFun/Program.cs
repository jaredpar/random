using DevOps.Util;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using DevOpsFun;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace QueryFun
{
    public class Program
    {
        public static string Organization = "dnceng";

        public static async Task Main(string[] args)
        {
            // await ListBuildsFullAsync();
            await UploadCloneTime();
            // await DumpCheckoutTimes("dnceng", "public", 196, top: 200);
            // Roslyn
            // await DumpCheckoutTimes("dnceng", "public", 15, top: 200);
            // Roslyn Integration
            // await DumpCheckoutTimes("dnceng", "public", 245, top: 200);
            // CoreFx
            // await DumpCheckoutTimes("dnceng", "public", 196, top: 200);
            // CoreClr
            // await DumpCheckoutTimes("dnceng", "public", 228, top: 200);
            // CLI
            // await DumpCheckoutTimes("dnceng", "public", 166, top: 200);
            // ASP.NET
            // await DumpCheckoutTimes("dnceng", "public", 278, top: 200);
            // await DumpTimelines("dnceng", "public", 15, top: 20);
            // await DumpTestTimes();
            // await UploadNgenData();
            // await DumpNgenData();
            // await DumpNgenData(2916584;
            // await Fun();
            // await DumpTimeline("public", 196140);
        }

        private static async Task<string> GetToken(string name)
        {
            var lines = await File.ReadAllLinesAsync(@"p:\tokens.txt");
            foreach (var line in lines)
            {
                var split = line.Split(':', count: 2);
                if (name == split[0])
                {
                    return split[1];
                }
            }

            throw new Exception($"Could not find token with name {name}");
        }

        private static async Task ListBuildsFullAsync()
        {
            var server = new DevOpsServer("dnceng");
            var builds1 = await server.ListBuildsAsync("public", new[] { 15 });
            var builds2 = await server.ListBuildsAsync("public", top: 10);
        }

        private static async Task UploadCloneTime()
        {
            using var util = new CloneTimeUtil(await GetToken("scratch-db"));
            await util.UpdateDatabaseAsync();
        }

        private static async Task DumpTestTimes()
        {
            using var util = new RunTestsUtil(await GetToken("scratch-db"));
            foreach (var build in (await util.ListBuildsAsync(top: 20)).Where(x => x.Result == BuildResult.Succeeded))
            {
                Console.Write(Util.GetUri(build));
                Console.Write(" ");
                try
                {
                    var buildTestTime = await util.GetBuildTestTimeAsync(build);
                    var milliseconds = buildTestTime.Jobs.Sum(x => x.Duration.TotalMilliseconds);
                    Console.Write(TimeSpan.FromMilliseconds(milliseconds));
                    Console.Write(" ");
                    var max = buildTestTime.Jobs.Max(x => x.Duration.TotalMilliseconds);
                    Console.WriteLine(TimeSpan.FromMilliseconds(max));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            await util.UpdateDatabaseAsync(top: 100);
        }

        private static async Task UploadNgenData()
        {
            var documentClient = new DocumentClient(
                new Uri("https://jaredpar-scratch.documents.azure.com:443/"),
                await GetToken("cosmos-db"));
            var ngenUtil = new NGenUtil(await GetToken("azure-devdiv"));
            var collectionUri = UriFactory.CreateDocumentCollectionUri("ngen", "ngen");
            foreach (var build in await ngenUtil.ListBuilds(top: 100))
            {
                if (build.Result != BuildResult.Succeeded)
                {
                    continue;
                }

                try
                {
                    Console.Write($"Getting build {build.Id} ... ");
                    var ngenDocument = await ngenUtil.GetNgenDocument(build);
                    var partitionKey = new PartitionKey(ngenDocument.Branch);
                    if (buildExists(build.Id, partitionKey))
                    {
                        Console.WriteLine("exists");
                        continue;
                    }

                    Console.Write($"uploading ...");
                    var response = await documentClient.CreateDocumentAsync(collectionUri, ngenDocument);
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        Console.WriteLine("succeeded");
                    }
                    else
                    {
                        Console.WriteLine($"failed {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("failed");
                    Console.WriteLine(ex.Message);
                }
            }

            bool buildExists(int buildId, PartitionKey partitionKey)
            {
                try
                {
                    var feedOptions = new FeedOptions()
                    {
                        PartitionKey = partitionKey,
                    };

                    var document1 = documentClient
                        .CreateDocumentQuery<NgenDocument>(collectionUri, feedOptions)
                        .Where(x => x.BuildId == buildId)
                        .AsEnumerable()
                        .ToList();

                    var document2 = documentClient
                        .CreateDocumentQuery<NgenDocument>(collectionUri, feedOptions)
                        .AsEnumerable()
                        .ToList();

                    var document = documentClient
                        .CreateDocumentQuery<NgenDocument>(collectionUri, feedOptions)
                        .Where(x => x.BuildId == buildId)
                        .AsEnumerable()
                        .FirstOrDefault();
                    return document is object;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static async Task DumpNgenData(int buildId)
        {
            var list = await GetNgenData(buildId);
            foreach (var data in list.OrderBy(x => x.AssemblyName))
            {
                Console.WriteLine($"{data.AssemblyName} - {data.MethodCount}");
            }
        }

        private static async Task DumpNgenData()
        {
            var server = new DevOpsServer("devdiv", await GetToken("azure-devdiv"));
            var project = "DevDiv";
            var data = new Dictionary<int, List<NgenEntryData>>();
            var comparer = StringComparer.OrdinalIgnoreCase;
            var assemblyNames = new HashSet<string>(comparer);
            foreach (var build in await server.ListBuildsAsync(project, new[] { 8972 }, top: 200))
            {
                if (build.Result == BuildResult.Succeeded && build.SourceBranch.Contains("master-vs-deps"))
                {
                    try
                    {
                        Console.WriteLine($"Getting data for {build.Uri}");
                        var list = await GetNgenData(build.Id);
                        data[build.Id] = list;
                        list.ForEach(x => assemblyNames.Add(x.AssemblyName));
                    }
                    catch
                    {
                        Console.WriteLine("Failed");
                    }
                }
            }

            var buildIds = data.Keys.OrderBy(x => x).ToList();
            Console.Write("Assembly Name,");
            buildIds.ForEach(x => Console.Write($"{x},"));
            Console.WriteLine();
            foreach (var assemblyName in assemblyNames.OrderBy(x => x))
            {
                Console.Write(assemblyName);
                Console.Write(',');
                foreach (var buildId in buildIds)
                {
                    try
                    {
                        var methodData = data[buildId].Single(x => comparer.Equals(x.AssemblyName, assemblyName));
                        Console.Write($"{methodData.MethodCount},");
                    }
                    catch
                    { 
                        Console.Write("N/A,");
                    }
                }
                Console.WriteLine();
            }
        }

        private static async Task<List<NgenEntryData>> GetNgenData(int buildId)
        {
            static int countLines(StreamReader reader)
            {
                var count = 0;
                while (null != reader.ReadLine())
                {
                    count++;
                }

                return count;
            }

            var server = new DevOpsServer("devdiv", await GetToken("azure-devdiv"));
            var project = "DevDiv";
            var stream = new MemoryStream();
            var regex = new Regex(@"(.*)-([\w.]+).ngen.txt", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            await server.DownloadArtifactAsync(project, buildId, "Build Diagnostic Files", stream);
            stream.Position = 0;
            using (var zipArchive = new ZipArchive(stream))
            {
                var list = new List<NgenEntryData>();
                foreach (var entry in zipArchive.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.Name) && entry.FullName.StartsWith("Build Diagnostic Files/ngen/"))
                    {
                        var match = regex.Match(entry.Name);
                        var assemblyName = match.Groups[1].Value;
                        var targetFramework = match.Groups[2].Value;
                        using var entryStream = entry.Open();
                        using var reader = new StreamReader(entryStream);
                        var methodCount = countLines(reader);

                        list.Add(new NgenEntryData(new NgenEntry(assemblyName, targetFramework), methodCount));
                    }
                }

                return list;
            }
        }

        private static async Task DumpNgenLogFun()
        {
            var server = new DevOpsServer("devdiv", await GetToken("azure-devdiv"));
            string project = "devdiv";
            var buildId = 2916584;
            var all = await server.ListArtifactsAsync(project, buildId);
            var buildArtifact = await server.GetArtifactAsync(project, buildId, "Build Diagnostic Files");
            var filePath = @"p:\temp\data.zip";
            await server.DownloadArtifactAsync(project, buildId, "Build Diagnostic Files", filePath);
        }

        private static async Task DumpTimelines(string organization, string project, int buildDefinitionId, int top)
        {
            var server = new DevOpsServer(organization);
            foreach (var build in await server.ListBuildsAsync(project, new[] { buildDefinitionId }, top: top))
            {
                Console.WriteLine($"{build.Id} {build.SourceBranch}");
                try
                {
                    await DumpTimeline(project, build.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static async Task DumpTimeline(string project, int buildId)
        {
            var server = new DevOpsServer(Organization);

            var timeline = await server.GetTimelineAsync(project, buildId);
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
                        var subTimeline = await server.GetTimelineAsync(project, buildId, record.Details.Id, record.Details.ChangeId);
                        await DumpTimeline(nextIndent, subTimeline);
                    }
                }
            }
        }

        private static async Task DumpCheckoutTimes(string organization, string project, int buildDefinitionId, int top)
        {
            var server = new DevOpsServer(organization);
            var total = 0;
            foreach (var build in await server.ListBuildsAsync(project, new[] { buildDefinitionId }, top: top))
            {
                var printed = false;
                void printBuildUri()
                {
                    if (!printed)
                    {
                        total++;
                        printed = true;
                        Console.WriteLine(Util.GetUri(build));
                    }
                }

                try
                {
                    var timeline = await server.GetTimelineAsync(project, build.Id);
                    if (timeline is null)
                    {
                        continue;
                    }

                    foreach (var record in timeline.Records.Where(x => x.Name == "Checkout" && x.FinishTime is object && x.StartTime is object))
                    {
                        var duration = DateTime.Parse(record.FinishTime) - DateTime.Parse(record.StartTime);
                        if (duration > TimeSpan.FromMinutes(10))
                        {
                            var parent = timeline.Records.Single(x => x.Id == record.ParentId);
                            printBuildUri();
                            Console.WriteLine($"\t{parent.Name} {duration}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine($"Total is {total}");
        }

        private static async Task DumpBuild(string project, int buildId)
        {
            var server = new DevOpsServer(Organization);
            var output = @"e:\temp\logs";
            Directory.CreateDirectory(output);
            foreach (var log in await server.GetBuildLogsAsync(project, buildId))
            {
                var logFilePath = Path.Combine(output, $"{log.Id}.txt");
                Console.WriteLine($"Log Id {log.Id} {log.Type} - {logFilePath}");
                var content = await server.GetBuildLogAsync(project, buildId, log.Id);
                File.WriteAllText(logFilePath, content);

            }
        }

        private static async Task Fun()
        { 
            var server = new DevOpsServer(Organization);
            var project = "public";
            var builds = await server.ListBuildsAsync(project, definitions: new[] { 15 }, top: 10);
            foreach (var build in builds)
            {
                Console.WriteLine($"{build.Id} {build.Uri}");
                var artifacts = await server.ListArtifactsAsync(project, build.Id);
                foreach (var artifact in artifacts)
                {
                    Console.WriteLine($"\t{artifact.Id} {artifact.Name} {artifact.Resource.Type}");
                }
            }
        }

        internal readonly struct NgenEntry
        {
            internal string AssemblyName { get; }
            internal string TargetFramework { get; }

            internal NgenEntry(string assemblyName, string targetFramework)
            {
                AssemblyName = assemblyName;
                TargetFramework = targetFramework;
            }
        }

        internal readonly struct NgenEntryData
        {
            internal NgenEntry NgenEntry { get; }
            internal int MethodCount { get; }

            internal string AssemblyName => NgenEntry.AssemblyName;
            internal string TargetFramework => NgenEntry.TargetFramework;

            internal NgenEntryData(NgenEntry ngenEntry, int methodCount)
            {
                NgenEntry = ngenEntry;
                MethodCount = methodCount;
            }
        }
    }
}


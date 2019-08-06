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

namespace QueryFun
{
    public class Program
    {
        public static string Organization = "dnceng";

        public static async Task Main(string[] args)
        {
            await DumpNgenData();
            // await DumpNgenData(2916584;
            // await Fun();
            // await DumpTimeline("public", 196140);
        }

        private static async Task<string> GetToken()
        {
            var text = await File.ReadAllLinesAsync(@"p:\tokens.txt");
            return text[0].Split(':')[1];
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
            var server = new DevOpsServer("devdiv", await GetToken());
            var project = "DevDiv";
            var data = new Dictionary<int, List<NgenEntryData>>();
            var comparer = StringComparer.OrdinalIgnoreCase;
            var assemblyNames = new HashSet<string>(comparer);
            foreach (var build in await server.ListBuilds(project, new[] { 8972 }, top: 30))
            {
                if (build.Result == BuildResult.Succeeded)
                {
                    Console.WriteLine($"Getting data for {build.Uri}");
                    var list = await GetNgenData(build.Id);
                    data[build.Id] = list;
                    list.ForEach(x => assemblyNames.Add(x.AssemblyName));
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

            var server = new DevOpsServer("devdiv", await GetToken());
            var project = "DevDiv";
            var stream = new MemoryStream();
            var regex = new Regex(@"(.*)-([\w.]+).ngen.txt", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            await server.DownloadArtifact(project, buildId, "Build Diagnostic Files", stream);
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
            var builds = await server.ListBuilds(project, definitions: new[] { 15 }, top: 10);
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


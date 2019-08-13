using DevOps.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DevOpsFun
{
    public sealed class NGenUtil
    {
        public const string ProjectName = "DevDiv";
        public const int RoslynSignedBuildDefinitionId = 8972;

        public DevOpsServer DevOpsServer { get; }

        public NGenUtil(string personalAccessToken)
        {
            DevOpsServer = new DevOpsServer("devdiv", personalAccessToken);
        }

        public async Task<Build[]> ListBuilds(int? top = null) =>
            await DevOpsServer.ListBuilds(ProjectName, definitions: new[] { RoslynSignedBuildDefinitionId }, top: top);

        public async Task<NgenDocument> GetNgenDocument(int buildId) =>
            await GetNgenDocument(await DevOpsServer.GetBuild(ProjectName, buildId));

        public async Task<NgenDocument> GetNgenDocument(Build build)
        { 
            if (build.Result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("The build didn't succeed");
            }

            var stream = new MemoryStream();
            var regex = new Regex(@"(.*)-([\w.]+).ngen.txt", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            await DevOpsServer.DownloadArtifact(ProjectName, build.Id, "Build Diagnostic Files", stream);
            stream.Position = 0;
            using (var zipArchive = new ZipArchive(stream))
            {
                var documentList = new List<AssemblyDocument>();
                foreach (var entry in zipArchive.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.Name) && entry.FullName.StartsWith("Build Diagnostic Files/ngen/"))
                    {
                        var match = regex.Match(entry.Name);
                        var assemblyName = match.Groups[1].Value;
                        var targetFramework = match.Groups[2].Value;
                        var methodList = new List<string>();
                        using var entryStream = entry.Open();
                        using var reader = new StreamReader(entryStream);

                        do
                        {
                            var line = reader.ReadLine();
                            if (line is null)
                            {
                                break;
                            }

                            methodList.Add(line);
                        }
                        while (true);

                        var document = new AssemblyDocument()
                        {
                            AssemblyName = assemblyName,
                            TargetFramework = targetFramework,
                            MethodCount = methodList.Count,
                        };
                        documentList.Add(document);
                    }
                }

                var branchName = Util.NormalizeBranchName(build.SourceBranch);
                return new NgenDocument()
                {
                    Branch = branchName,
                    BuildId = build.Id,
                    Assemblies = documentList.ToArray()
                };
            }
        }
    }

    public sealed class NgenDocument
    {
        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("buildId")]
        public int BuildId { get; set; }

        [JsonProperty("assemblies")]
        public AssemblyDocument[] Assemblies { get; set; }

        public override string ToString() => $"{Branch} - {BuildId}";
    }

    public sealed class AssemblyDocument
    {
        [JsonProperty("assemblyName")]
        public string AssemblyName { get; set; }

        [JsonProperty("targetFramework")]
        public string TargetFramework { get; set; }

        [JsonProperty("methodCount")]
        public int MethodCount { get; set; }

        public override string ToString() => AssemblyName;
    }
}

using DevOps.Util;
using DevOps.Util.DotNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DevOps.Util.DotNet
{
    public sealed class NGenUtil
    {
        public const string ProjectName = "DevDiv";
        public const int RoslynSignedBuildDefinitionId = 8972;

        public DevOpsServer DevOpsServer { get; }
        public ILogger Logger { get; }

        public NGenUtil(string personalAccessToken, ILogger logger = null)
        {
            Logger = logger ?? Util.CreateConsoleLogger();
            DevOpsServer = new DevOpsServer("devdiv", personalAccessToken);
        }

        public async Task<List<Build>> ListBuildsAsync(int? top = null) =>
            await DevOpsServer.ListBuildsAsync(ProjectName, definitions: new[] { RoslynSignedBuildDefinitionId }, top: top);

        public async Task<List<NGenAssemblyData>> GetNGenAssemblyDataAsync(int buildId) =>
            await GetNGenAssemblyDataAsync(await DevOpsServer.GetBuildAsync(ProjectName, buildId));

        public async Task<List<NGenAssemblyData>> GetNGenAssemblyDataAsync(Build build)
        {
            var uri = Util.GetUri(build);
            Logger.LogInformation($"Processing {build.Id} - {uri}");
            if (build.Result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("The build didn't succeed");
            }

            // Newer builds have the NGEN logs in a separate artifact altogether to decrease the time needed 
            // to downloaad them. Try that first and fall back to the diagnostic logs if it doesn't exist.
            MemoryStream stream;
            Func<ZipArchiveEntry, bool> predicate;

            try
            {
                Logger.LogInformation("Downloading NGEN logs");
                stream = await DevOpsServer.DownloadArtifactAsync(ProjectName, build.Id, "NGen Logs");
                predicate = e => !string.IsNullOrEmpty(e.Name);
            }
            catch (Exception)
            {
                Logger.LogInformation("Falling back to diagnostic logs");
                stream = await DevOpsServer.DownloadArtifactAsync(ProjectName, build.Id, "Build Diagnostic Files");
                predicate = e => !string.IsNullOrEmpty(e.Name) && e.FullName.StartsWith("Build Diagnostic Files/ngen/");
            }

            return await GetFromStream(stream, predicate);
        }

        private async Task<List<NGenAssemblyData>> GetFromStream(Stream stream, Func<ZipArchiveEntry, bool> predicate)
        { 
            var regex = new Regex(@"(.*)-([\w.]+).ngen.txt", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            using var zipArchive = new ZipArchive(stream);
            var list = new List<NGenAssemblyData>();
            foreach (var entry in zipArchive.Entries)
            {
                if (predicate(entry))
                {
                    var match = regex.Match(entry.Name);
                    var assemblyName = match.Groups[1].Value;
                    var targetFramework = match.Groups[2].Value;
                    var methodList = new List<string>();
                    using var entryStream = entry.Open();
                    using var reader = new StreamReader(entryStream);

                    do
                    {
                        var line = await reader.ReadLineAsync();
                        if (line is null)
                        {
                            break;
                        }

                        methodList.Add(line);
                    }
                    while (true);

                    var ngenAssemblyData = new NGenAssemblyData(assemblyName, targetFramework, methodList.Count);
                    list.Add(ngenAssemblyData);
                }
            }

            return list;
        }
    }
}

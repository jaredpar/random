using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOps.Util;

internal sealed class RuntimeInfo
{
    internal static readonly (string BuildName, int buildId)[] BuildDefinitions = new[] 
        {
            ("runtime", 686),
            ("coreclr", 655),
            ("libraries", 675),
            ("libraries windows", 676),
            ("libraries linux", 677),
            ("libraries osx", 678),
            ("crossgen2", 701)
        };

    internal DevOpsServer Server;

    internal RuntimeInfo(string personalAccessToken = null)
    {
        Server = new DevOpsServer("dnceng", personalAccessToken);
    }

    internal async Task PrintBuildResults()
    {
        foreach (var (name, definitionId) in BuildDefinitions)
        {
            Console.Write($"{name,-20}");
            foreach (var result in await GetBuildResultsAsync(Server, definitionId, count: 5))
            {
                var c = result == BuildResult.Succeeded ? 'Y' : 'N';
                Console.Write(c);
            }

            Console.WriteLine();
        }

        static async Task<List<BuildResult>> GetBuildResultsAsync(DevOpsServer server, int definitionId, int count)
        {
            var builds = await server.ListBuildsAsync(
                "public",
                new[] { definitionId },
                statusFilter: BuildStatus.Completed,
                queryOrder: BuildQueryOrder.FinishTimeDescending,
                top: count * 20);
            var filteredBuilds = builds
                .Where(x => x.Reason != BuildReason.PullRequest)
                .Take(count);
            var list = new List<BuildResult>(capacity: count);
            foreach (var build in filteredBuilds)
            {
                list.Add(build.Result);
            }

            return list;
        }
    }
}
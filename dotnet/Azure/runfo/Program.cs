using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOps.Util;

public class Program
{

    internal static async Task Main(string[] args)
    {
        var server = new DevOpsServer("dnceng");
        var definitions = new[] 
        {
            ("pr / ci", 686),
            ("coreclr", 655),
            ("libraries", 675),
            ("libraries windows", 676),
            ("libraries linux", 677),
            ("libraries osx", 678),
            ("crossgen2", 701)
        };

        foreach (var (name, definitionId) in definitions)
        {
            Console.Write($"{name,-20}");
            foreach (var result in await GetBuildResultsAsync(server, definitionId, count: 5))
            {
                var c = result == BuildResult.Succeeded ? 'Y' : 'N';
                Console.Write(c);
            }

            Console.WriteLine();
        }
    }

    private static async Task<List<BuildResult>> GetBuildResultsAsync(DevOpsServer server, int definitionId, int count)
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

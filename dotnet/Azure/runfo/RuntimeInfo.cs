using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOps.Util;
using DevOps.Util.DotNet;
using Mono.Options;
using static RuntimeInfoUtil;

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
            foreach (var build in await GetBuildResultsAsync("public", definitionId, count: 5))
            {
                var c = build.Result == BuildResult.Succeeded ? 'Y' : 'N';
                Console.Write(c);
            }

            Console.WriteLine();
        }

    }

    internal async Task<int> PrintBuilds(IEnumerable<string> args)
    {
        string definition = null;
        int count = 5;
        var optionSet = new OptionSet()
        {
            { "d|definition=", "definition to print tests for", d => definition = d },
            { "c|count=", "count of builds to return", (int c) => count = c }
        };

        ParseAll(optionSet, args);

        if (!TryGetDefinitionId(definition, out int definitionId))
        {
            Console.WriteLine($"{definition} is not a valid build definition name / id");
            PrintDefinitions();
            optionSet.WriteOptionDescriptions(Console.Out);
            return ExitFailure;
        }

        foreach (var build in await GetBuildResultsAsync("public", definitionId, count))
        {
            var uri = Util.GetUri(build);
            Console.WriteLine($"{build.Id}\t{build.Result}\t{uri}");
        }

        return ExitSuccess;
    }

    internal async Task<int> PrintFailedTests(IEnumerable<string> args)
    {
        int? buildId = 0;
        var optionSet = new OptionSet()
        {
            { "b|build=", "build id to print tests for", (int b) => buildId = b }
        };

        ParseAll(optionSet, args);

        if (buildId is null)
        {
            Console.WriteLine("Need a build number");
            optionSet.WriteOptionDescriptions(Console.Out);
            return ExitFailure;
        }

        await PrintFailedTest(buildId.Value);
        return ExitSuccess;
    }

    private async Task PrintFailedTest(int buildId)
    {
        var testRuns = await Server.ListTestRunsAsync("public", buildId);
        foreach (var testRun in testRuns)
        {
            var all = await Server.ListTestResultsAsync("public", testRun.Id, outcomes: new[] { TestOutcome.Failed });
            if (all.Length == 0)
            {
                continue;
            }

            Console.WriteLine(testRun.Name);
            foreach (var testCaseResult in all)
            {
                Console.WriteLine($"\t{testCaseResult.TestCaseTitle}");
                if (testCaseResult.FailingSince.Build.Id != buildId)
                {
                    var days = DateTime.UtcNow - DateTime.Parse(testCaseResult.FailingSince.Date);
                    Console.WriteLine($"\t{testCaseResult.TestCaseTitle} {days.TotalDays}");
                }
            }
        }
    }

    private static void PrintDefinitions()
    {
        Console.WriteLine("Supported definition names");
        foreach (var (name, id) in BuildDefinitions)
        {
            Console.WriteLine($"{id}\t{name}");
        }
    }

    private bool TryGetDefinitionId(string definition, out int definitionId)
    {
        definitionId = 0;
        if (definition is null)
        {
            return false;
        }

        if (int.TryParse(definition, out definitionId))
        {
            return true;
        }

        foreach (var (name, id) in BuildDefinitions)
        {
            if (name == definition)
            {
                definitionId = id;
                return true;
            }
        }

        return false;
    }

    private async Task<List<Build>> GetBuildResultsAsync(string project, int definitionId, int count)
    {
        var builds = await Server.ListBuildsAsync(
            project,
            new[] { definitionId },
            statusFilter: BuildStatus.Completed,
            queryOrder: BuildQueryOrder.FinishTimeDescending,
            top: count * 20);
        return builds
            .Where(x => x.Reason != BuildReason.PullRequest)
            .Take(count)
            .ToList();
    }

    private static void ParseAll(OptionSet optionSet, IEnumerable<string> args)
    {
        var extra = optionSet.Parse(args);
        if (extra.Count != 0)
        {
            optionSet.WriteOptionDescriptions(Console.Out);
            var text = string.Join(' ', extra);
            throw new Exception($"Extra arguments: {text}");
        }
    }
}
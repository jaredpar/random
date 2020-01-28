using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevOps.Util;
using DevOps.Util.DotNet;
using Mono.Options;
using static RuntimeInfoUtil;

internal sealed class RuntimeInfo
{
    internal static readonly (string BuildName, int DefinitionId)[] BuildDefinitions = new[] 
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

    internal async Task PrintBuildResults(IEnumerable<string> args)
    {
        int count = 5;
        var optionSet = new OptionSet()
        {
            { "c|count=", "count of builds to return", (int c) => count = c }
        };

        ParseAll(optionSet, args); 

        var data = BuildDefinitions
            .AsParallel()
            .AsOrdered()
            .Select(async t => (t.BuildName, t.DefinitionId, await GetBuildResultsAsync("public", t.DefinitionId, count)));

        foreach (var task in data)
        {
            var (name, definitionId, builds) = await task;
            Console.Write($"{name,-20}");
            var percent = (builds.Count(x => x.Result == BuildResult.Succeeded) / (double)count) * 100;
            Console.Write($"{percent,4:G3}%  ");
            foreach (var build in builds)
            {
                var c = build.Result == BuildResult.Succeeded ? 'Y' : 'N';
                Console.Write(c);
            }

            Console.WriteLine();
        }
    }

    internal void PrintBuildDefinitions()
    {
        foreach (var (name, definitionId) in BuildDefinitions)
        {
            var uri = DevOpsUtil.GetBuildDefinitionUri(Server.Organization, "public", definitionId);
            Console.WriteLine($"{name,-20}{uri}");
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
            OptionFailureDefinition(definition, optionSet);
            return ExitFailure;
        }

        foreach (var build in await GetBuildResultsAsync("public", definitionId, count))
        {
            var uri = DevOpsUtil.GetBuildUri(build);
            Console.WriteLine($"{build.Id}\t{build.Result}\t{uri}");
        }

        return ExitSuccess;
    }

    internal async Task<int> PrintFailedTests(IEnumerable<string> args)
    {
        int? buildId = null;
        int count = 5;
        bool verbose = false;
        string definition = null;
        string grouping = "builds";
        var optionSet = new OptionSet()
        {
            { "b|build=", "build id to print tests for", (int b) => buildId = b },
            { "d|definition=", "build definition name / id", d => definition = d },
            { "c|count=", "count of builds to show for a definition", (int c) => count = c},
            { "g|grouping=", "output grouping: builds*, tests, jobs", g => grouping = g },
            { "v|verbose", "verobes output", d => verbose = d is object }
        };

        ParseAll(optionSet, args);

        if (buildId is object && definition is object)
        {
            OptionFailure("Cannot specified build and definition", optionSet);
            return ExitFailure;
        }

        if (buildId is null && definition is null)
        {
            OptionFailure("Need either a build or definition", optionSet);
            return ExitFailure;
        }

        if (definition is object)
        {
            if (!TryGetDefinitionId(definition, out int definitionId))
            {
                OptionFailureDefinition(definition, optionSet);
                return ExitFailure;
            }

            await PrintFailedTestsForDefinition("public", definitionId, count, grouping, verbose);
            return ExitSuccess;
        }

        Debug.Assert(buildId is object);
        await PrintFailedTests(buildId.Value);
        return ExitSuccess;
    }

    private async Task PrintFailedTestsForDefinition(string project, int definitionId, int count, string grouping, bool verbose)
    {
        switch (grouping)
        {
            case "tests":
                await GroupByTests();
                break;
            case "builds":
                await GroupByBuilds();
                break;
            case "jobs":
                await GroupByJobs();
                break;
            default:
                throw new Exception($"{grouping} is not a valid grouping");
        }

        async Task GroupByBuilds()
        {
            foreach (var build in await GetBuildResultsAsync(project, definitionId, count))
            {
                Console.WriteLine($"{build.Id} {DevOpsUtil.GetBuildUri(build)}");
                await PrintFailedTests(build.Id, indent: "\t");
            }
        }

        async Task GroupByTests()
        {
            var buildTestInfoList = await GetTestResultsAsync(project, definitionId, count);
            var all = buildTestInfoList.SelectMany(x => x.GetTestCaseTitles()).Distinct().ToList();
            foreach (var testCaseTitle in all)
            {
                var testRunList = buildTestInfoList
                    .SelectMany(x => x.GetTestResultsForTestCaseTitle(testCaseTitle))
                    .OrderBy(x => x.TestRun.Name)
                    .ToList();
                Console.WriteLine($"{testCaseTitle} {testRunList.Count}");
                if (verbose)
                {
                    Console.WriteLine($"{GetIndent(1)}Builds");
                    foreach (var build in buildTestInfoList.Where(x => x.ContainsTestCaseTitle(testCaseTitle)).Select(x => x.Build).OrderByDescending(x => x.Id))
                    {
                        var uri = DevOpsUtil.GetBuildUri(build);
                        Console.WriteLine($"{GetIndent(2)}{uri}");
                    }

                    Console.WriteLine($"{GetIndent(1)}Test Runs");
                    foreach (var (testRun, _) in testRunList)
                    {
                        var count = testRunList.Count(t => t.TestRun.Name == testRun.Name);
                        Console.WriteLine($"{GetIndent(2)}{count}\t{testRun.Name}");
                    }
                }
            }
        }

        async Task GroupByJobs()
        {
            var buildTestInfoList = await GetTestResultsAsync(project, definitionId, count);
            var testRunNames = buildTestInfoList
                .SelectMany(x => x.GetTestRuns().Select(x => x.Name))
                .Distinct()
                .OrderBy(x => x);
            foreach (var testRunName in testRunNames)
            {
                var list = buildTestInfoList.Where(x => x.ContainsTestRunName(testRunName));
                Console.WriteLine($"{testRunName}");
                if (verbose)
                {
                    Console.WriteLine($"{GetIndent(1)}Builds");
                    foreach (var build in list)
                    {
                        var uri = DevOpsUtil.GetBuildUri(build.Build);
                        Console.WriteLine($"{GetIndent(2)}{uri}");
                    }

                    Console.WriteLine($"{GetIndent(1)}Test Cases");
                    var testCaseTitles = list
                        .SelectMany(x => x.GetTestResultsForTestRunName(testRunName))
                        .Select(x => x.TestCaseTitle)
                        .Distinct()
                        .OrderBy(x => x);
                    foreach (var testCaseTitle in testCaseTitles)
                    {
                        var count = list
                            .SelectMany(x => x.GetTestResultsForTestCaseTitle(testCaseTitle))
                            .Count(x => x.TestRun.Name == testRunName);
                        Console.WriteLine($"{GetIndent(2)}{testCaseTitle} ({count})");
                    }
                }
                else
                {
                    var buildCount = list.Count();
                    var testCaseCount = list.Sum(x => x.GetTestResultsForTestRunName(testRunName).Count());
                    Console.WriteLine($"{GetIndent(1)}Builds {buildCount}");
                    Console.WriteLine($"{GetIndent(1)}Test Cases {testCaseCount}");
                }
            }
        }
    }

    private async Task PrintFailedTests(int buildId, string indent = "")
    {
        var testRuns = await Server.ListTestRunsAsync("public", buildId);
        foreach (var testRun in testRuns)
        {
            var all = await Server.ListTestResultsAsync("public", testRun.Id, outcomes: new[] { TestOutcome.Failed });
            if (all.Length == 0)
            {
                continue;
            }

            Console.WriteLine($"{indent}{testRun.Name}");
            foreach (var testCaseResult in all)
            {
                Console.WriteLine($"{indent}\t{testCaseResult.TestCaseTitle}");
                if (testCaseResult.FailingSince.Build.Id != buildId)
                {
                    var days = DateTime.UtcNow - DateTime.Parse(testCaseResult.FailingSince.Date);
                    Console.WriteLine($"{indent}\t{testCaseResult.TestCaseTitle} {days.TotalDays}");
                }
            }
        }
    }

    private async Task<List<BuildTestInfo>> GetTestResultsAsync(string project, int definitionId, int count)
    {
        var list = new List<BuildTestInfo>();
        foreach (var build in await GetBuildResultsAsync(project, definitionId, count))
        {
            list.Add(await GetTestResultsAsync(build));
        }

        return list;
    }

    private async Task<BuildTestInfo> GetTestResultsAsync(Build build)
    {
        var taskList = new List<Task<(TestRun, List<TestCaseResult>)?>>();
        var testRuns = await Server.ListTestRunsAsync("public", build.Id);
        foreach (var testRun in testRuns)
        {
            var task = GetTestRunResultsAsync(testRun);
            taskList.Add(task);
        }

        await Task.WhenAll(taskList);
        var list = taskList
            .Where(x => x.Result.HasValue)
            .Select(x => x.Result.Value)
            .OrderBy(x => x.Item1.Id)
            .ToList();
        return new BuildTestInfo(build, list);

        async Task<(TestRun, List<TestCaseResult>)?> GetTestRunResultsAsync(TestRun testRun)
        {
            var all = await Server.ListTestResultsAsync("public", testRun.Id, outcomes: new[] { TestOutcome.Failed });
            if (all.Length == 0)
            {
                return null;
            }

            return (testRun, all.ToList());
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

    private static void OptionFailure(string message, OptionSet optionSet)
    {
        Console.WriteLine(message);
        optionSet.WriteOptionDescriptions(Console.Out);
    }

    private static void OptionFailureDefinition(string definition, OptionSet optionSet)
    {
        Console.WriteLine($"{definition} is not a valid definition name or id");
        Console.WriteLine("Supported definition names");
        foreach (var (name, id) in BuildDefinitions)
        {
            Console.WriteLine($"{id}\t{name}");
        }

        optionSet.WriteOptionDescriptions(Console.Out);
    }

    private static string GetIndent(int level) => level == 0 ? string.Empty : new string(' ', level * 2);
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

    internal async Task<int> PrintHelix(IEnumerable<string> args)
    {
        int? buildId = null;;
        var verbose = false;
        var optionSet = new OptionSet()
        {
            { "b|build=", "build to print out", (int b) => buildId = b },
            { "v|verbose", "verbose output", v => verbose = v is object },
        };

        ParseAll(optionSet, args); 
        if (buildId is null)
        {
            Console.WriteLine("Build id (-b) is required");
            optionSet.WriteOptionDescriptions(Console.Out);
            return ExitFailure;
        }

        var buildResultInfo = await GetBuildTestInfoAsync(buildId.Value);
        var logs = buildResultInfo
            .GetHelixWorkItems()
            .AsParallel()
            .Select(async t => await GetHelixLogInfoAsync(t))
            .Select(async (Task<HelixLogInfo> task) => {
                var helixLogInfo = await task;
                string consoleText = null;
                if (verbose && helixLogInfo.ConsoleUri is object)
                {
                    consoleText = await HelixUtil.GetHelixConsoleText(Server, helixLogInfo.ConsoleUri);
                }
                return (helixLogInfo, consoleText);
            });

        var list = await RuntimeInfoUtil.ToList(logs);

        Console.WriteLine("Console Logs");
        foreach (var (helixLogInfo, consoleText) in list.Where(x => x.Item1.ConsoleUri is object))
        {
            Console.WriteLine($"{helixLogInfo.ConsoleUri}");
            if (verbose)
            {
                Console.WriteLine(consoleText);
            }
        }

        Console.WriteLine();
        var wroteHeader = false;
        foreach (var (helixLogInfo, _) in list.Where(x => x.helixLogInfo.TestResultsUri is object))
        {
            if (!wroteHeader)
            {
                Console.WriteLine("Test Results");
                wroteHeader = true;
            }
            Console.WriteLine($"{helixLogInfo.TestResultsUri}");
        }

        Console.WriteLine();
        wroteHeader = false;
        foreach (var (helixLogInfo, _) in list.Where(x => x.helixLogInfo.CoreDumpUri is object))
        {
            if (!wroteHeader)
            {
                Console.WriteLine("Core Logs");
                wroteHeader = true;
            }
            Console.WriteLine($"{helixLogInfo.CoreDumpUri}");
        }
        return ExitSuccess;
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
        var includePullRequests = false;
        var optionSet = new OptionSet()
        {
            { "d|definition=", "definition to print tests for", d => definition = d },
            { "c|count=", "count of builds to return", (int c) => count = c },
            { "pr", "include pull requests", p => includePullRequests = p is object },
        };

        ParseAll(optionSet, args);

        if (!TryGetDefinitionId(definition, out int definitionId))
        {
            OptionFailureDefinition(definition, optionSet);
            return ExitFailure;
        }

        foreach (var build in await GetBuildResultsAsync("public", definitionId, count, includePullRequests))
        {
            var uri = DevOpsUtil.GetBuildUri(build);
            var prId = DevOpsUtil.GetPullRequestNumber(build);
            var kind = prId.HasValue ? "PR" : "CI";
            Console.WriteLine($"{build.Id}\t{kind}\t{build.Result,-13}\t{uri}");
        }

        return ExitSuccess;
    }

    internal async Task<int> PrintFailedTests(IEnumerable<string> args)
    {
        int? buildId = null;
        int count = 5;
        bool verbose = false;
        bool markdown = false;
        bool includePullRequests = false;
        string definition = null;
        string name = null;
        string grouping = "builds";
        var optionSet = new OptionSet()
        {
            { "b|build=", "build id to print tests for", (int b) => buildId = b },
            { "d|definition=", "build definition name / id", d => definition = d },
            { "c|count=", "count of builds to show for a definition", (int c) => count = c},
            { "g|grouping=", "output grouping: builds*, tests, jobs", g => grouping = g },
            { "pr", "include pull requests", p => includePullRequests = p is object },
            { "m|markdown", "output in markdown", m => markdown = m  is object },
            { "n|name=", "name regex to match in results", n => name = n },
            { "v|verbose", "verobes output", d => verbose = d is object }
        };

        ParseAll(optionSet, args);

        BuildTestInfoCollection collection;
        if (buildId is object)
        {
            if (definition is object)
            {
                OptionFailure("Cannot specified build and definition", optionSet);
                return ExitFailure;
            }

            var buildTestInfo = await GetBuildTestInfoAsync(buildId.Value);
            collection = new BuildTestInfoCollection(new[] { buildTestInfo });
        }
        else if (definition is object)
        {
            if (!TryGetDefinitionId(definition, out int definitionId))
            {
                OptionFailureDefinition(definition, optionSet);
                return ExitFailure;
            }

            collection = await ListBuildTestInfosAsync("public", definitionId, count, includePullRequests);
        }
        else
        {
            OptionFailure("Need either a build or definition", optionSet);
            return ExitFailure;
        }

        if (!string.IsNullOrEmpty(name))
        {
            var regex = new Regex(name, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            collection = collection.FilterToTestCaseTitle(regex);
        }

        await PrintFailedTestsForDefinition(collection, grouping, verbose, markdown);
        return ExitSuccess;
    }

    private async Task PrintFailedTestsForDefinition(
        BuildTestInfoCollection collection,
        string grouping,
        bool verbose,
        bool markdown)
    {
        switch (grouping)
        {
            case "tests":
                await GroupByTests();
                break;
            case "builds":
                GroupByBuilds();
                break;
            case "jobs":
                GroupByJobs();
                break;
            default:
                throw new Exception($"{grouping} is not a valid grouping");
        }

        void GroupByBuilds()
        {
            foreach (var buildTestInfo in collection)
            {
                PrintFailedTests(buildTestInfo);
            }
        }

        async Task GroupByTests()
        {
            if (markdown)
            {
                await GroupByTestsMarkdown(collection);
            }
            else
            {
                GroupByTestsConsole(collection);
            }
        }

        void GroupByTestsConsole(BuildTestInfoCollection collection)
        {
            foreach (var testCaseTitle in collection.GetTestCaseTitles())
            {
                var testRunList = collection.GetHelixTestRunResultsForTestCaseTitle(testCaseTitle);
                Console.WriteLine($"{testCaseTitle} {testRunList.Count}");
                if (verbose)
                {
                    Console.WriteLine($"{GetIndent(1)}Builds");
                    foreach (var build in collection.GetBuildsForTestCaseTitle(testCaseTitle))
                    {
                        var uri = DevOpsUtil.GetBuildUri(build);
                        Console.WriteLine($"{GetIndent(2)}{uri}");
                    }

                    Console.WriteLine($"{GetIndent(1)}Test Runs");
                    foreach (var helixTestRunResult in testRunList)
                    {
                        var testRun = helixTestRunResult.TestRun;
                        var count = testRunList.Count(t => t.TestRun.Name == testRun.Name);
                        Console.WriteLine($"{GetIndent(2)}{count}\t{testRun.Name}");
                    }
                }
            }
        }

        async Task GroupByTestsMarkdown(BuildTestInfoCollection collection)
        {
            foreach (var testCaseTitle in collection.GetTestCaseTitles())
            {
                Console.WriteLine($"## {testCaseTitle}");
                Console.WriteLine("");
                Console.WriteLine("### Console Log Summary");
                Console.WriteLine("");
                Console.WriteLine("### Builds");
                Console.WriteLine("|Build|Pull Request | Test Failure Count|");
                Console.WriteLine("| --- | --- | --- |");
                foreach (var buildTestInfo in collection.GetBuildTestInfosForTestCaseTitle(testCaseTitle))
                {
                    var build = buildTestInfo.Build;
                    var uri = DevOpsUtil.GetBuildUri(build);
                    var pr = GetPullRequestColumn(build);
                    var testFailureCount = buildTestInfo.GetHelixTestRunResultsForTestCaseTitle(testCaseTitle).Count();
                    Console.WriteLine($"|[#{build.Id}]({uri})|{pr}|{testFailureCount}|");
                }

                Console.WriteLine($"### Configurations");
                foreach (var testRunName in collection.GetTestRunNamesForTestCaseTitle(testCaseTitle))
                {
                    Console.WriteLine($"- {EscapeAtSign(testRunName)}");
                }

                Console.WriteLine($"### Helix Logs");
                Console.WriteLine("|Build|Pull Request|Console|Core|Test Results|");
                Console.WriteLine("| --- | --- | --- | --- | --- |");
                foreach (var (build, helixLogInfo) in await GetHelixLogs(collection, testCaseTitle))
                {
                    var uri = DevOpsUtil.GetBuildUri(build);
                    var pr = GetPullRequestColumn(build);
                    Console.Write($"|[#{build.Id}]({uri})|{pr}");
                    PrintUri(helixLogInfo.ConsoleUri, "console");
                    PrintUri(helixLogInfo.CoreDumpUri, "core");
                    PrintUri(helixLogInfo.TestResultsUri, "testResults.xml");
                    Console.WriteLine("|");
                }

                static void PrintUri(string uri, string defaultDisplayName)
                {
                    if (uri is null)
                    {
                        Console.Write("|");
                        return;
                    }
                    
                    try
                    {
                        if (Uri.TryCreate(uri, UriKind.Absolute, out var realUri))
                        {
                            var name = Path.GetFileName(realUri.LocalPath);
                            Console.Write($"|[{name}]({uri})");
                            return;
                        }
                    }
                    catch
                    {
                        // Badly formatted URI
                    }

                    Console.Write($"|[{defaultDisplayName}]({uri})");
                }

                static string EscapeAtSign(string text) => text.Replace("@", "@<!-- -->");

                static string GetPullRequestColumn(Build build)
                {
                    var prNumber = DevOpsUtil.GetPullRequestNumber(build);
                    if (prNumber is null)
                    {
                        return "Rolling";
                    }

                    return $"#{prNumber.Value}";
                }

                Console.WriteLine();
            }
        }

        void GroupByJobs()
        {
            var testRunNames = collection.GetTestRunNames();
            foreach (var testRunName in testRunNames)
            {
                var list = collection.Where(x => x.ContainsTestRunName(testRunName));
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
                        .SelectMany(x => x.GetHelixTestRunResultsForTestRunName(testRunName))
                        .Select(x => x.HelixTestResult.TestCaseTitle)
                        .Distinct()
                        .OrderBy(x => x);
                    foreach (var testCaseTitle in testCaseTitles)
                    {
                        var count = list
                            .SelectMany(x => x.GetHelixTestRunResultsForTestCaseTitle(testCaseTitle))
                            .Count(x => x.TestRun.Name == testRunName);
                        Console.WriteLine($"{GetIndent(2)}{testCaseTitle} ({count})");
                    }
                }
                else
                {
                    var buildCount = list.Count();
                    var testCaseCount = list.Sum(x => x.GetHelixTestRunResultsForTestRunName(testRunName).Count());
                    Console.WriteLine($"{GetIndent(1)}Builds {buildCount}");
                    Console.WriteLine($"{GetIndent(1)}Test Cases {testCaseCount}");
                }
            }
        }
    }

    private static void PrintFailedTests(BuildTestInfo buildTestInfo)
    {
        var build = buildTestInfo.Build;
        Console.WriteLine($"{build.Id} {DevOpsUtil.GetBuildUri(build)}");
        foreach (var testRunName in buildTestInfo.GetTestRunNames())
        {
            Console.WriteLine($"{GetIndent(1)}{testRunName}");
            foreach (var testResult in buildTestInfo.GetHelixTestRunResultsForTestRunName(testRunName))
            {
                var suffix = "";
                var testCaseResult = testResult.HelixTestResult.Test;
                if (testCaseResult.FailingSince.Build.Id != build.Id)
                {
                    suffix = $"(since {testCaseResult.FailingSince.Build.Id})";
                }
                Console.WriteLine($"{GetIndent(2)}{testCaseResult.TestCaseTitle} {suffix}");
            }
        }
    }

    private async Task<List<(Build, HelixLogInfo)>> GetHelixLogs(BuildTestInfoCollection collection, string testCaseTitle)
    {
        var query = collection
            .GetHelixTestRunResultsForTestCaseTitle(testCaseTitle)
            .OrderBy(x => x.Build.Id)
            .ToList()
            .AsParallel()
            .AsOrdered()
            .Select(async testRunResult => {
                var helixLogInfo = await GetHelixLogInfoAsync(testRunResult);
                return (testRunResult.Build, helixLogInfo);
            });
        var list = await RuntimeInfoUtil.ToList(query);
        return list;
    }

    private async Task<BuildTestInfoCollection> ListBuildTestInfosAsync(string project, int definitionId, int count, bool includePullRequests = false)
    {
        var list = new List<BuildTestInfo>();
        foreach (var build in await GetBuildResultsAsync(project, definitionId, count, includePullRequests))
        {
            try
            {
                list.Add(await GetBuildTestInfoAsync(build));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot get test info for {build.Id} {DevOpsUtil.GetBuildUri(build)}");
                Console.WriteLine(ex.Message);
            }
        }

        return new BuildTestInfoCollection(new ReadOnlyCollection<BuildTestInfo>(list));
    }

    private async Task<BuildTestInfo> GetBuildTestInfoAsync(int buildId)
    {
        var build = await Server.GetBuildAsync("public", buildId);
        return await GetBuildTestInfoAsync(build);
    }

    private async Task<BuildTestInfo> GetBuildTestInfoAsync(Build build)
    {
        var taskList = new List<Task<(TestRun, List<TestCaseResult>)?>>();
        var testRuns = await Server.ListTestRunsAsync("public", build.Id);
        foreach (var testRun in testRuns)
        {
            var task = GetTestRunResultsAsync(testRun);
            taskList.Add(task);
        }

        await Task.WhenAll(taskList);

        var list = new List<HelixTestRunResult>();
        foreach (var task in taskList)
        {
            var tuple = task.Result;
            if (!tuple.HasValue)
            {
                continue;
            }

            var testCaseResults = tuple.Value.Item2;
            foreach (var testCaseResult in testCaseResults)
            {
                HelixTestResult helixTestResult;
                if (HelixUtil.IsHelixWorkItem(testCaseResult))
                {
                    helixTestResult = new HelixTestResult(testCaseResult);
                }
                else
                {
                    var workItem = testCaseResults.FirstOrDefault(x => HelixUtil.IsHelixWorkItemAndTestCaseResult(workItem: x, test: testCaseResult));
                    helixTestResult = new HelixTestResult(test: testCaseResult, workItem: workItem);
                }

                list.Add(new HelixTestRunResult(build, tuple.Value.Item1, helixTestResult));
            }
        }

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

    private async Task<List<Build>> GetBuildResultsAsync(string project, int definitionId, int count, bool includePullRequests = false)
    {
        IEnumerable<Build> builds = await Server.ListBuildsAsync(
            project,
            new[] { definitionId },
            statusFilter: BuildStatus.Completed,
            queryOrder: BuildQueryOrder.FinishTimeDescending,
            top: count * 20);

        if (!includePullRequests)
        {
            builds = builds.Where(x => x.Reason != BuildReason.PullRequest);
        }

        return builds
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

    // The logs for the failure always exist on the associated work item, not on the 
    // individual test result
    private async Task<HelixLogInfo> GetHelixLogInfoAsync(HelixTestRunResult testRunResult) => 
        await HelixUtil.GetHelixLogInfoAsync(Server, "public", testRunResult.TestRun.Id, testRunResult.HelixTestResult.WorkItem.Id);

    private static string GetIndent(int level) => level == 0 ? string.Empty : new string(' ', level * 2);
}
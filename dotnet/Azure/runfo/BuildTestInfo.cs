using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevOps.Util;


// TODO: make this type use actual dictionaries and hashes instead of crappy lists.
// just wrote this for functionality at the moment. Perf fix ups later.
internal sealed class BuildTestInfo
{
    public List<(TestRun TestRun, List<TestCaseResult> Failed)> DataList;

    public Build Build { get; }

    internal BuildTestInfo(Build build, List<(TestRun TestRun, List<TestCaseResult> Failed)> dataList)
    {
        Build = build;
        DataList = dataList;
    }

    internal IEnumerable<string> GetTestCaseTitles() => DataList
        .SelectMany(x => x.Failed)
        .Select(x => x.TestCaseTitle)
        .Distinct()
        .OrderBy(x => x);

    internal IEnumerable<TestRun> GetTestRuns() => DataList.Select(x => x.TestRun);

    internal IEnumerable<TestRun> GetTestRunsForTestCaseTitle(string testCaseTitle) => DataList
        .Where(x => x.Failed.Exists(x => x.TestCaseTitle == testCaseTitle))
        .Select(x => x.TestRun);

    internal IEnumerable<string> GetTestRunNamesForTestCaseTitle(string testCaseTitle) => this
        .GetTestRunsForTestCaseTitle(testCaseTitle)
        .Select(x => x.Name)
        .Distinct()
        .OrderBy(x => x);

    internal IEnumerable<(TestRun TestRun, TestCaseResult TestCaseResult)> GetTestResultsForTestCaseTitle(string testCaseTitle) => DataList
        .SelectMany(x => x.Failed.Select(y => (x.TestRun, y)))
        .Where(x => x.y.TestCaseTitle == testCaseTitle)
        .ToList();

    internal IEnumerable<TestCaseResult> GetTestResultsForTestRunName(string testRunName) => DataList
        .Where(x => x.TestRun.Name == testRunName)
        .SelectMany(x => x.Failed);

    public bool ContainsTestCaseTitle(string testCaseTitle) => GetTestCaseTitles().Contains(testCaseTitle);

    public bool ContainsTestRunName(string testRunName) => DataList.Exists(x => x.TestRun.Name == testRunName);
}

internal sealed class BuildTestInfoCollection : IEnumerable<BuildTestInfo>
{
    public ReadOnlyCollection<BuildTestInfo> BuildTestInfos { get; }

    public BuildTestInfoCollection(ReadOnlyCollection<BuildTestInfo> buildTestInfos)
    {
        BuildTestInfos = buildTestInfos;
    }

    public BuildTestInfoCollection(IEnumerable<BuildTestInfo> buildTestInfos)
        : this(new ReadOnlyCollection<BuildTestInfo>(buildTestInfos.ToList()))
    {

    }

    public List<string> GetTestCaseTitles() => BuildTestInfos
        .SelectMany(x => x.GetTestCaseTitles())
        .Distinct()
        .ToList();

    internal List<(TestRun TestRun, TestCaseResult TestCaseResult)> GetTestResultsForTestCaseTitle(string testCaseTitle) => BuildTestInfos
        .SelectMany(x => x.GetTestResultsForTestCaseTitle(testCaseTitle))
        .OrderBy(x => x.TestRun.Name)
        .ToList();

    internal List<Build> GetBuildsForTestCaseTitle(string testCaseTitle) => this
        .GetBuildTestInfosForTestCaseTitle(testCaseTitle)
        .Select(x => x.Build)
        .ToList();

    internal List<BuildTestInfo> GetBuildTestInfosForTestCaseTitle(string testCaseTitle) => BuildTestInfos
        .Where(x => x.ContainsTestCaseTitle(testCaseTitle))
        .OrderBy(x => x.Build.Id)
        .ToList();

    internal List<TestRun> GetTestRunsForTestCaseTitle(string testCaseTitle) => BuildTestInfos
        .SelectMany(x => x.GetTestRunsForTestCaseTitle(testCaseTitle))
        .OrderBy(x => x.Id)
        .ToList();

    internal List<string> GetTestRunNamesForTestCaseTitle(string testCaseTitle) => BuildTestInfos
        .SelectMany(x => x.GetTestRunNamesForTestCaseTitle(testCaseTitle))
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    internal List<string> GetTestRunNames() => BuildTestInfos
        .SelectMany(x => x.GetTestRuns().Select(x => x.Name))
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    public IEnumerator<BuildTestInfo> GetEnumerator() => BuildTestInfos.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}







using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevOps.Util;


// TODO: make this type use actual dictionaries and hashes instead of crappy lists.
// just wrote this for functionality at the moment. Perf fix ups later.
internal sealed class BuildTestInfo
{
    private readonly List<(TestRun TestRun, List<TestCaseResult> Failed)> _dataList;

    public Build Build { get; }

    internal BuildTestInfo(Build build, List<(TestRun TestRun, List<TestCaseResult> Failed)> dataList)
    {
        Build = build;
        _dataList = dataList;
    }

    internal IEnumerable<string> GetTestCaseTitles() => _dataList
        .SelectMany(x => x.Failed)
        .Select(x => x.TestCaseTitle)
        .Distinct()
        .OrderBy(x => x);

    internal IEnumerable<TestRun> GetTestRuns() => _dataList.Select(x => x.TestRun);

    internal IEnumerable<(TestRun TestRun, TestCaseResult TestCaseResult)> GetTestResultsForTestCaseTitle(string testCaseTitle) => _dataList
        .SelectMany(x => x.Failed.Select(y => (x.TestRun, y)))
        .Where(x => x.y.TestCaseTitle == testCaseTitle)
        .ToList();

    internal IEnumerable<TestCaseResult> GetTestResultsForTestRunName(string testRunName) => _dataList
        .Where(x => x.TestRun.Name == testRunName)
        .SelectMany(x => x.Failed);

    public bool ContainsTestCaseTitle(string testCaseTitle) => GetTestCaseTitles().Contains(testCaseTitle);

    public bool ContainsTestRunName(string testRunName) => _dataList.Exists(x => x.TestRun.Name == testRunName);
}






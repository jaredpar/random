using System;
using Mono.Options;

internal sealed class BuildSearchOptionSet : OptionSet
{
    public int? BuildId { get; set; }

    public int BuildCount { get; set; } = 5;

    public string Definition { get; set; }

    public string Repository { get; set; }

    public DateTime? Before { get; set; }

    public DateTime? After { get; set; }

    public bool IncludePullRequests { get; set; }

    public string Project { get; set; } = "public";

    public BuildSearchOptionSet()
    {
        Add("b|build=", "build id to print tests for", (int b) => BuildId = b);
        Add("d|definition=", "build definition name / id", d => Definition = d);
        Add("c|count=", "count of builds to show for a definition", (int c) => BuildCount = c);
        Add("pr", "include pull requests", p => IncludePullRequests = p is object);
        Add("before=", "filter to builds before this date", (DateTime d) => Before = d);
        Add("after=", "filter to builds after this date", (DateTime d) => After = d);
        Add("p|project=", "project to search is (public)", p => Project = p);
        Add("r|repository=", "repository to search against", r => Repository = r);
    }
}

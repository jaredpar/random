using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiTest
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            Go().GetAwaiter().GetResult();
        }

        internal static async Task Go()
        { 
            var items = File.ReadAllText(@"c:\users\jaredpar\jenkins.txt").Trim().Split(':');
            var credentials = new Credentials(items[1]);
            var client = new GitHubClient(new ProductHeaderValue("jaredpar-api-test"));
            client.Connection.Credentials = credentials;

            var request = new RepositoryIssueRequest();
            request.Labels.Add("Area-Compilers");
            request.State = ItemState.Open;
            request.Milestone = "4";

            var issues = await client.Issue.GetAllForRepository("dotnet", "roslyn", request);
            foreach (var issue in issues)
            {
                var name = issue.User.Name ?? "unassigned";
                Console.WriteLine(issue.Url);
            }
        }
    }
}

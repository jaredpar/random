using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bugs
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Go().Wait();
        }

        private static async Task Go()
        {
            var githubClient = new GitHubClient(new ProductHeaderValue("Bugs"));

            var colon = ':';
            var q = $@"repo{colon}dotnet/roslyn+label{colon}Area-Compilers+milestone{colon}1.1+is{colon}open";
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            map[nameof(q)] = q;
            map["page"] = "2";

            var apiConnection = new ApiConnection(githubClient.Connection);
            var apiUrl = ApiUrls.SearchIssues();
            var html = await apiConnection.GetHtml(apiUrl, map);
            var result = await apiConnection.Get<SearchIssuesResult>(apiUrl, map);
            foreach (var cur in result.Items)
            {
                Console.WriteLine(cur.Title);
            }
        }
    }
}

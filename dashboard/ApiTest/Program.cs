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

            var all = await client.Issue.Milestone.GetAllForRepository("dotnet", "roslyn");
            foreach (var cur in all)
            {
                Console.WriteLine(cur.Title);
            }
        }
    }
}

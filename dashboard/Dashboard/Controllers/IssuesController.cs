using Dashboard.Models;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class IssuesController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var items = System.IO.File.ReadAllText(@"c:\users\jaredpar\jenkins.txt").Trim().Split(':');
            var token = items[1];

            var client = new GitHubClient(new ProductHeaderValue("jbug-dash-app"));
            client.Credentials = new Credentials(token);

            var request = new RepositoryIssueRequest();
            request.Labels.Add("Area-Compilers");
            request.State = ItemState.Open;
            request.Milestone = "4";

            var issues = await client.Issue.GetAllForRepository("dotnet", "roslyn", request);
            var model = new IssuesViewModel();
            foreach (var issue in issues)
            {
                var name = issue?.Assignee?.Login ?? "unassigned";
                model.Issues.Add(new IssueData() { Id = issue.Number, User = name });
            }

            return View(model);
        }
    }
}
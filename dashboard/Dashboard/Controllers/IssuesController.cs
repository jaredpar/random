using Dashboard.Models;
using Octokit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class IssuesController : Controller
    {
        private static GitHubClient CreateClient()
        {
            var token = ConfigurationManager.AppSettings["github-token"];
            var client = new GitHubClient(new ProductHeaderValue("jbug-dash-app"));
            client.Credentials = new Credentials(token);
            return client;
        }

        public Task<ActionResult> Index()
        {
            return Area("Area-Compilers");
        }

        public async Task<ActionResult> Area(string areaLabel = "")
        { 
            var client = CreateClient();
            var request = new RepositoryIssueRequest();
            request.Labels.Add(areaLabel);
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
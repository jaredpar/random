using Dashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dashboard.Controllers
{
    public class IssuesController : Controller
    {
        public ActionResult Index()
        {
            var model = new IssuesViewModel();
            model.Issues.Add(new Issue() { Id = 42, Url = "blah" });
            model.Issues.Add(new Issue() { Id = 13, Url = "blah" });
            return View(model);
        }
    }
}
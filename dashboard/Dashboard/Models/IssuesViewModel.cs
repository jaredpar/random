using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class IssuesViewModel
    {
        public List<Issue> Issues { get; } = new List<Issue>();
    }

    public sealed class Issue
    {
        public int Id { get; set; }
        public string Url { get; set; }
    }
}
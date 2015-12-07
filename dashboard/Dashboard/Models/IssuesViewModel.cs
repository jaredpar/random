using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class IssuesViewModel
    {
        public List<IssueData> Issues { get; } = new List<IssueData>();
    }

    public sealed class IssueData
    {
        public int Id { get; set; }
        public string Url { get; set; }
    }
}
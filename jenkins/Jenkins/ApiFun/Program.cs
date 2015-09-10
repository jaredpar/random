using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApiFun
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var client = new JenkinsClient();
            var builds = client.GetJobIds(Platform.Windows).Take(2);
            var jobInfoList = builds.Select(x => client.GetJobInfo(x)).ToList();

            var data = jobInfoList.GroupBy(x => $"{x.PullRequestInfo.Id} - {x.PullRequestInfo.Sha1}");
            foreach (var cur in data)
            {
                var all = cur.ToList();
                if (all.Count == 1)
                {
                    continue;
                }

                Console.WriteLine(cur.Key);
            }
        }
    }

}

using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFun
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            GetWindowsPullJobs();
        }

        private static List<Build> GetWindowsPullJobs()
        {
            var client = new RestClient("http://dotnet-ci.cloudapp.net");
            var request = new RestRequest("job/dotnet_roslyn_prtest_win/api/json", Method.GET);
            request.AddParameter("pretty", "true");
            var content = client.Execute(request).Content;
            var data = JObject.Parse(content);
            var all = (JArray)data["builds"];

            var list = new List<Build>();
            foreach (var cur in all)
            {
                list.Add(cur.ToObject<Build>());
            }

            return list;
        }
    }
}

using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSql
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var connectionString = File.ReadAllText(@"c:\users\jaredpar\connection.txt");
            var client = new DataClient(connectionString);
            // PopulateAllJobInfos(client);
            // PopulateAllFailures(client);
        }

        private static void PopulateAllJobInfos(DataClient client)
        {
            foreach (var id in client.Client.GetJobIds())
            {
                try
                {
                    Console.Write($"Processing {id.Id} {id.Platform} ... ");
                    var info = client.Client.GetJobInfo(id);
                    client.InsertJobInfo(info);
                    Console.WriteLine("Done");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR!!!");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void PopulateAllFailures(DataClient client)
        {
            foreach (var id in client.Client.GetJobIds())
            {
                try
                {
                    Console.Write($"Processing {id.Id} {id.Platform} ... ");
                    var jobResult = client.Client.GetJobResult(id);
                    if (!jobResult.Failed)
                    {
                        Console.WriteLine("Succeeded");
                        continue;
                    }

                    client.InsertFailure(jobResult.JobInfo, jobResult.FailureInfo);

                    Console.WriteLine("Done");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR!!!");
                    Console.WriteLine(ex.Message);
                }


            }
        }

        private static void PopulateAllRetest(DataClient client)
        {

        }
    }
}

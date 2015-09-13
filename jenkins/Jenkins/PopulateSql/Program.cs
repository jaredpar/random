using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
            var client = new JenkinsClient();
            var connectionString = File.ReadAllText(@"c:\users\jaredpar\connection.txt");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                foreach (var jobId in client.GetJobIds(Platform.Windows))
                {
                    var jobInfo = client.GetJobInfo(jobId);
                    if (jobInfo.State == JobState.Running)
                    {
                        continue;
                    }

                    Insert(connection, jobInfo);
                }
            }
        }

        internal static void Insert(SqlConnection connection, JobInfo jobInfo)
        {
            var id = jobInfo.JobId;
            var commandText = @"
                INSERT INTO dbo.Jobs (Id, JobId, Platform, Date, Sha, PullRequestId, Succeeded)
                VALUES (@Id, @JobId, @Platform, @Date, @Sha, @PullRequestId, @Succeeded)";
            using (var command = new SqlCommand(commandText, connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", $"{id.Date}_{id.Id}_{id.Platform}");
                p.AddWithValue("@JobId", jobInfo.JobId.Id);
                p.AddWithValue("@Platform", id.Platform.ToString());
                p.AddWithValue("@Date", id.Date);
                p.AddWithValue("@Sha", jobInfo.PullRequestInfo.Sha1);
                p.AddWithValue("@PullRequestId", jobInfo.PullRequestInfo.Id);
                p.AddWithValue("@Succeeded", jobInfo.State == JobState.Succeeded);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not insert {jobInfo.JobId.Id}");
                    Console.WriteLine(ex.Message);
                }

            }
        }
    }
}

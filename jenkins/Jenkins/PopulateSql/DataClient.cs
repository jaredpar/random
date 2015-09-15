using Roslyn.Jenkins;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSql
{
    internal sealed class DataClient : IDisposable
    {
        private readonly JenkinsClient _client = new JenkinsClient();
        private SqlConnection _connection;

        internal JenkinsClient Client => _client;

        internal DataClient(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        internal void InsertJobInfo(JobInfo jobInfo)
        {
            var id = jobInfo.JobId;
            var commandText = @"
                INSERT INTO dbo.Jobs (Id, JobId, Platform, Date, Sha, PullRequestId, Succeeded)
                VALUES (@Id, @JobId, @Platform, @Date, @Sha, @PullRequestId, @Succeeded)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", id.Key);
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

        internal void InsertFailure(JobInfo info, JobFailureInfo failureInfo)
        {
            var commandText = @"
                INSERT INTO dbo.Failures (Id, Sha, Reason, Messages)
                VALUES (@Id, @Sha, @Reason, @Messages)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", info.JobId.Key);
                p.AddWithValue("@Sha", info.PullRequestInfo.Sha1);
                p.AddWithValue("@Reason", failureInfo.Reason.ToString());
                p.AddWithValue("@Messages", string.Join(";", failureInfo.Messages));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not insert failure {info.JobId}");
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}

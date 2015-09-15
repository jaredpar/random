using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class DataClient : IDisposable
    {
        private SqlConnection _connection;

        public DataClient(string connectionString)
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

        public bool HasSucceeded(Platform platform, string sha)
        {
            var commandText = @"
                SELECT Count(*)
                FROM Jobs
                WHERE Succeeded=1 AND Sha=@SHA AND Platform=@Platform";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@SHA", sha);
                p.AddWithValue("@Platform", platform.ToString());

                var count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        public int GetPullRequestId(UniqueJobId id)
        {
            var commandText = @"
                SELECT PullRequestId
                FROM Jobs
                WHERE Id=@Id";
            using (var command = new SqlCommand(commandText, _connection))
            {
                command.Parameters.AddWithValue("@Id", id.Key);

                var list = new List<Tuple<UniqueJobId, string>>();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new Exception("Missing data");
                    }

                    return reader.GetInt32(0);
                }
            }
        }

        public JobFailureInfo GetFailureInfo(UniqueJobId id)
        {
            var commandText = @"
                SELECT Reason,Messages 
                FROM Failures
                WHERE Id=@Id";
            using (var command = new SqlCommand(commandText, _connection))
            {
                command.Parameters.AddWithValue("@Id", id.Key);

                var list = new List<Tuple<UniqueJobId, string>>();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new Exception("Missing data");
                    }

                    var reason = reader.GetString(0);
                    var messages = reader.GetString(1).Split(';').ToList();
                    return new JobFailureInfo(
                        (JobFailureReason)(Enum.Parse(typeof(JobFailureReason), reason)),
                        messages);
                }
            }
        }

        public List<Tuple<UniqueJobId, string>> GetFailures()
        {
            var commandText = @"
                SELECT Id,Sha 
                FROM Failures";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<Tuple<UniqueJobId, string>>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        var id = UniqueJobId.TryParse(key).Value;
                        var sha = reader.GetString(1);
                        list.Add(Tuple.Create(id, sha));
                    }
                }

                return list;
            }
        }

        public List<RetestInfo> GetRetestInfo()
        {
            var commandText = @"
                SELECT Id,Sha,Handled,Note
                FROM Retests";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var list = new List<RetestInfo>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        var sha = reader.GetString(1);
                        var handled = reader.GetBoolean(2);
                        var note = reader.IsDBNull(3)
                            ? null
                            : reader.GetString(3);
                        var info = new RetestInfo(
                            UniqueJobId.TryParse(key).Value,
                            sha,
                            handled,
                            note);
                        list.Add(info);
                    }
                }

                return list;
            }
        }

        public void InsertJobInfo(JobInfo jobInfo)
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

        public void InsertFailure(JobInfo info, JobFailureInfo failureInfo)
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

        public void InsertRetest(UniqueJobId jobId, string sha)
        {
            var commandText = @"
                INSERT INTO dbo.Retests (Id, Sha, Handled)
                VALUES (@Id, @Sha, @Handled)";
            using (var command = new SqlCommand(commandText, _connection))
            {
                var p = command.Parameters;
                p.AddWithValue("@Id", jobId.Key);
                p.AddWithValue("@Sha", sha);
                p.AddWithValue("@Handled", 0);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not insert retest {jobId}");
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}

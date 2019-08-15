using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DevOps.Util.DotNet
{
    /// <summary>
    /// Utility for logging clone times
    /// </summary>
    public sealed class CloneTimeUtil : IDisposable
    {
        public const string ProjectName = "public";
        public const int BuildDefinitionId = 15;

        public SqlConnection SqlConnection { get; }
        public DevOpsServer DevOpsServer { get; }
        public ILogger Logger { get; }

        public CloneTimeUtil(string sqlConnectionString, ILogger logger = null)
        {
            Logger = logger ?? Util.CreateConsoleLogger();
            DevOpsServer = new DevOpsServer("dnceng");
            SqlConnection = new SqlConnection(sqlConnectionString);
        }

        public void Dispose()
        {
            SqlConnection.Dispose();
        }

        public async Task<List<Build>> ListBuildsAsync(int top) => await DevOpsServer.ListBuildsAsync(ProjectName, new[] { BuildDefinitionId }, top: top);

        public async Task UpdateDatabaseAsync()
        {
            var builds = await DevOpsServer.ListBuildsAsync(ProjectName, definitions: new[] { 15 });
            foreach (var build in builds)
            {
                try
                {
                    await UploadBuild(build);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unable to upload {build.Id}: {ex.Message}");
                }
            }
        }

        public async Task<bool> IsBuildUploadedAsync(int buildId)
        {
            if (SqlConnection.State != ConnectionState.Open)
            {
                await SqlConnection.OpenAsync();
            }

            var query = "SELECT * FROM dbo.BuildCloneTime WHERE BuildId = @BuildId";
            using var command = new SqlCommand(query, SqlConnection);
            command.Parameters.AddWithValue("@BuildId", buildId);
            using var reader = await command.ExecuteReaderAsync();
            return reader.HasRows;
        }

        public async Task UploadBuildAsync(int buildId)
        {
            var build = await DevOpsServer.GetBuildAsync(ProjectName, buildId);
            await UploadBuild(build);
        }

        private async Task UploadBuild(Build build)
        {
            try
            {
                var uri = Util.GetUri(build);
                if (await IsBuildUploadedAsync(build.Id))
                {
                    Logger.LogInformation($"Build already uploaded {uri}");
                    return;
                }

                Logger.LogInformation($"Getting timeline {uri}");
                var jobs = await GetJobCloneTimesAsync(build);
                if (jobs.Count == 0)
                {
                    Logger.LogInformation("Found no jobs");
                    return;
                }

                Logger.LogInformation($"Uploading {uri}");
                var buildStartTime = DateTimeOffset.Parse(build.StartTime);

                await Util.DoWithTransactionAsync(SqlConnection, $"Upload Clone {build.Id}", async transaction =>
                {
                    foreach (var tuple in jobs)
                    {
                        await UploadJobCloneTime(transaction, build.Id, build.Definition.Id, tuple.JobName, tuple.Duration, buildStartTime, uri);
                    }

                    var minDuration = jobs.Min(x => x.Duration);
                    var maxDuration = jobs.Max(x => x.Duration);
                    await UploadBuildCloneTime(transaction, build.Id, build.Definition.Id, minDuration, maxDuration, buildStartTime, uri);
                });
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error {ex.Message}");
                throw;
            }
        }

        private async Task<List<(string JobName, TimeSpan Duration)>> GetJobCloneTimesAsync(Build build)
        {
            var list = new List<(string JobName, TimeSpan Duration)>();
            var timeline = await DevOpsServer.GetTimelineAsync(ProjectName, build.Id);
            if (timeline is null)
            {
                return list;
            }

            foreach (var record in timeline.Records.Where(x => x.Name == "Checkout" && x.FinishTime is object && x.StartTime is object))
            {
                var duration = DateTime.Parse(record.FinishTime) - DateTime.Parse(record.StartTime);
                var parent = timeline.Records.Single(x => x.Id == record.ParentId);
                list.Add((parent.Name, duration));
            }

            return list;
        }

        private async Task UploadJobCloneTime(SqlTransaction transaction, int buildId, int definitionId, string jobName, TimeSpan duration, DateTimeOffset buildStartTime, Uri buildUri)
        {
            var query = "INSERT INTO dbo.JobCloneTime (BuildId, DefinitionId, Name, Duration, BuildStartTime, BuildUri) VALUES (@BuildId, @DefinitionId, @Name, @Duration, @BuildStartTime, @BuildUri)";
            using var command = new SqlCommand(query, transaction.Connection, transaction);
            command.Parameters.AddWithValue("@BuildId", buildId);
            command.Parameters.AddWithValue("@DefinitionId", definitionId);
            command.Parameters.AddWithValue("@Name", jobName);
            command.Parameters.AddWithValue("@Duration", duration);
            command.Parameters.AddWithValue("@BuildStartTime", buildStartTime);
            command.Parameters.AddWithValue("@BuildUri", buildUri.ToString());
            var result = await command.ExecuteNonQueryAsync();
            if (result < 0)
            {
                throw new Exception("Unable to execute the insert");
            }
        }

        private async Task UploadBuildCloneTime(SqlTransaction transaction, int buildId, int definitionId, TimeSpan minDuration, TimeSpan maxDuration, DateTimeOffset buildStartTime, Uri buildUri)
        {
            var query = "INSERT INTO dbo.BuildCloneTime (BuildId, DefinitionId, MinDuration, MaxDuration, BuildStartTime, BuildUri) VALUES (@BuildId, @DefinitionId, @MinDuration, @MaxDuration, @BuildStartTime, @BuildUri)";
            using var command = new SqlCommand(query, transaction.Connection, transaction);
            command.Parameters.AddWithValue("@BuildId", buildId);
            command.Parameters.AddWithValue("@DefinitionId", definitionId);
            command.Parameters.AddWithValue("@MinDuration", minDuration);
            command.Parameters.AddWithValue("@MaxDuration", maxDuration);
            command.Parameters.AddWithValue("@BuildStartTime", buildStartTime);
            command.Parameters.AddWithValue("@BuildUri", buildUri.ToString());
            var result = await command.ExecuteNonQueryAsync();
            if (result < 0)
            {
                throw new Exception("Unable to execute the insert");
            }
       }
    }
}

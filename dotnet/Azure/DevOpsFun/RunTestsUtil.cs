using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using DevOps.Util;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Azure.Documents;
using System.Data.SqlClient;
using System.Data;

namespace DevOpsFun
{
    /// <summary>
    /// Utility for parsing out our test results
    /// </summary>
    public sealed class RunTestsUtil : IDisposable
    {
        public const string ProjectName = "public";
        public const int BuildDefinitionId = 15;

        public SqlConnection SqlConnection { get; }
        public DevOpsServer DevOpsServer { get; }

        public RunTestsUtil(string sqlConnectionString)
        {
            DevOpsServer = new DevOpsServer("dnceng");
            SqlConnection = new SqlConnection(sqlConnectionString);
        }

        public void Dispose()
        {
            SqlConnection.Dispose();
        }

        public async Task<List<TimeSpan>> GetTestTimes(int top)
        {
            var list = new List<TimeSpan>();
            foreach (var build in await DevOpsServer.ListBuilds(ProjectName, new[] { BuildDefinitionId }, top: top))
            {
                if (build.Result == BuildResult.Succeeded)
                {
                    list.Add(await GetTestTime(build));
                }
            }

            return list;
        }

        public async Task<TimeSpan> GetTestTime(Build build)
        {
            if (build.Result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException();
            }

            using var stream = new MemoryStream();
            await DevOpsServer.DownloadBuildLogs(ProjectName, build.Id, stream);
            stream.Position = 0;

            using (var zipArchive = new ZipArchive(stream))
            {
                var entry = zipArchive
                    .Entries
                    .FirstOrDefault(x => x.FullName == "Windows_Desktop_Unit_Tests debug_32/3_Build and Test.txt");
                if (entry is null)
                {
                    throw new Exception($"Could not find the log file");
                }

                var regex = new Regex(@"Test execution time: ([\d.:]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                do
                {
                    var line = reader.ReadLine();
                    if (line is null)
                    {
                        break;
                    }
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        return TimeSpan.Parse(match.Groups[1].Value);
                    }
                } while (true);

                throw new Exception("Could not parse the log");
            }
        }

        public async Task UpdateDatabase(int top)
        {
            foreach (var build in await DevOpsServer.ListBuilds(ProjectName, new[] { BuildDefinitionId }, top: top))
            {
                if (build.Result == BuildResult.Succeeded)
                {
                    Console.WriteLine($"Processing {build.Id}");
                    await UpdateDatabase(build);
                }
            }
        }

        public async Task UpdateDatabase(Build build)
        {
            if (build.Result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException();
            }

            if (SqlConnection.State != ConnectionState.Open)
            {
                await SqlConnection.OpenAsync();
            }

            using var stream = new MemoryStream();
            await DevOpsServer.DownloadBuildLogs(ProjectName, build.Id, stream);
            stream.Position = 0;

            using (var zipArchive = new ZipArchive(stream))
            {
                foreach (var entry in zipArchive.Entries.Where(x => x.Name == "3_Build and Test.txt"))
                {
                    var jobName = entry.FullName.Split("/")[0];
                    var jobKind = GetJobKind(jobName);
                    if (jobKind.HasValue)
                    {
                        var regex = new Regex(@"Test execution time: ([\d.:]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        using var entryStream = entry.Open();
                        using var reader = new StreamReader(entryStream);
                        do
                        {
                            var line = reader.ReadLine();
                            if (line is null)
                            {
                                throw new Exception("Could not parse the log");
                            }

                            var match = regex.Match(line);
                            if (match.Success)
                            {
                                var duration = match.Groups[1].Value;
                                await UploadJobTestTime(build.Id, jobKind.Value, TimeSpan.Parse(duration));
                                break;
                            }
                        } while (true);
                    }
                }
            }
        }

        private async Task UploadJobTestTime(int buildId, int jobKind, TimeSpan duration)
        {
            var query = "INSERT INTO dbo.JobTestTime (BuildId, JobKind, Duration) VALUES (@BuildId, @JobKind, @Duration)";
            using var command = new SqlCommand(query, SqlConnection);
            command.Parameters.AddWithValue("@BuildId", buildId);
            command.Parameters.AddWithValue("@JobKind", jobKind);
            command.Parameters.AddWithValue("@Duration", duration);
            var result = await command.ExecuteNonQueryAsync();
            if (result < 0)
            {
                throw new Exception("Unable to execute the insert");
            }
        }

        private static int? GetJobKind(string jobName) => jobName.ToLower() switch
        {
            "windows_desktop_unit_tests debug_32" => 1,
            _ => (int?)null
        };
    }
}

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
using System.Reflection;

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

        public async Task UpdateDatabaseAsync(int top)
        {
            foreach (var build in await DevOpsServer.ListBuilds(ProjectName, new[] { BuildDefinitionId }, top: top))
            {
                if (build.Result == BuildResult.Succeeded)
                {
                    try
                    {
                        Console.Write($"Processing {build.Id} {build.SourceBranch}");
                        await UpdateDatabaseAsync(build);
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed");
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public async Task UpdateDatabaseAsync(Build build)
        {
            if (build.Result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException();
            }

            if (SqlConnection.State != ConnectionState.Open)
            {
                await SqlConnection.OpenAsync();
            }

            var buildTestTime = await GetBuildTestTimeAsync(build);

            foreach (var job in buildTestTime.Jobs)
            {
                var jobKind = GetJobKind(job.JobName);
                if (jobKind.HasValue)
                {
                    foreach (var assembly in job.Assemblies)
                    {
                        await UploadAssemblyTestTime(build.Id, jobKind.Value, buildTestTime.BranchName, assembly.AssemblyName, assembly.Duration);
                    }

                    await UploadJobTestTime(build.Id, jobKind.Value, buildTestTime.BranchName, job.Duration);
                }
            }
        }

        private async Task<BuildTestTime> GetBuildTestTimeAsync(Build build)
        {
            if (build.Result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException();
            }

            using var stream = new MemoryStream();
            await DevOpsServer.DownloadBuildLogs(ProjectName, build.Id, stream);
            stream.Position = 0;
            return GetBuildTestTime(build, stream);
        }

        private BuildTestTime GetBuildTestTime(Build build, Stream logStream)
        {
            var totalTimeRegex = new Regex(@"Test execution time: ([\d.:]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var assemblyTimeRegex = new Regex(@" ([\w.]+\.dll[\d.]*)\s+PASSED ([\d.:]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var branchName = BranchName.Parse(build.SourceBranch);
            using var zipArchive = new ZipArchive(logStream);
            var jobs = new List<JobTestTime>();
            foreach (var entry in zipArchive.Entries.Where(x => x.Name == "3_Build and Test.txt"))
            {
                var jobName = entry.FullName.Split("/")[0];
                var assemblies = new List<AssemblyTestTime>();
                TimeSpan? jobDuration = null;

                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                do
                {
                    var line = reader.ReadLine();
                    if (line is null)
                    {
                        break;
                    }

                    var assemblyTimeMatch = assemblyTimeRegex.Match(line);
                    if (assemblyTimeMatch.Success)
                    {
                        var duration = TimeSpan.Parse(assemblyTimeMatch.Groups[2].Value);
                        assemblies.Add(new AssemblyTestTime(assemblyTimeMatch.Groups[1].Value, duration));
                        continue;
                    }

                    var totalTimeMatch = totalTimeRegex.Match(line);
                    if (totalTimeMatch.Success)
                    {
                        jobDuration = TimeSpan.Parse(totalTimeMatch.Groups[1].Value);
                        break;
                    }

                } while (true);

                if (jobDuration is object)
                {
                    jobs.Add(new JobTestTime(jobName, jobDuration.Value, assemblies));
                }
            }

            if (jobs.Count < 4)
            {
                throw new Exception("Could not parse the log");
            }

            return new BuildTestTime(build.Id, branchName, jobs);
        }

        private async Task UploadJobTestTime(int buildId, int jobKind, BranchName branchName, TimeSpan duration)
        {
            var query = "INSERT INTO dbo.JobTestTime (BuildId, JobKind, Branch, IsPullRequest, Duration) VALUES (@BuildId, @JobKind, @Branch, @IsPullRequest, @Duration)";
            using var command = new SqlCommand(query, SqlConnection);
            command.Parameters.AddWithValue("@BuildId", buildId);
            command.Parameters.AddWithValue("@JobKind", jobKind);
            command.Parameters.AddWithValue("@Branch", branchName.FullName);
            command.Parameters.AddWithValue("@IsPullRequest", branchName.IsPullRequest);
            command.Parameters.AddWithValue("@Duration", duration);
            var result = await command.ExecuteNonQueryAsync();
            if (result < 0)
            {
                throw new Exception("Unable to execute the insert");
            }
        }

        private async Task UploadAssemblyTestTime(int buildId, int jobKind, BranchName branchName, string assemblyName, TimeSpan duration)
        {
            var query = "INSERT INTO dbo.AssemblyTime (BuildId, JobKind, AssemblyName, Branch, IsPullRequest, Duration) VALUES (@BuildId, @JobKind, @AssemblyName, @Branch, @IsPullRequest, @Duration)";
            using var command = new SqlCommand(query, SqlConnection);
            command.Parameters.AddWithValue("@BuildId", buildId);
            command.Parameters.AddWithValue("@JobKind", jobKind);
            command.Parameters.AddWithValue("@AssemblyName", assemblyName);
            command.Parameters.AddWithValue("@Branch", branchName.FullName);
            command.Parameters.AddWithValue("@IsPullRequest", branchName.IsPullRequest);
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
            "windows_desktop_unit_tests debug_64" => 2,
            "windows_desktop_unit_tests release_32" => 3,
            "windows_desktop_unit_tests release_64" => 4,
            _ => (int?)null
        };
    }

    public sealed class BuildTestTime
    {
        public int BuildId { get; }
        public BranchName BranchName { get; }
        public List<JobTestTime> Jobs { get; }

        public BuildTestTime(int buildId, BranchName branchName, List<JobTestTime> jobs)
        {
            BuildId = buildId;
            BranchName = branchName;
            Jobs = jobs;
        }

        public override string ToString() => $"{BuildId} - {BranchName.FullName}";
    }

    public sealed class JobTestTime
    {
        public string JobName { get; }
        public TimeSpan Duration { get; }
        public List<AssemblyTestTime> Assemblies { get; }

        public JobTestTime(string jobName, TimeSpan duration, List<AssemblyTestTime> assemblies)
        {
            JobName = jobName;
            Duration = duration;
            Assemblies = assemblies;
        }

        public override string ToString() => JobName;
    }

    public sealed class AssemblyTestTime
    {
        public string AssemblyName { get; }
        public TimeSpan Duration { get; }

        public AssemblyTestTime(string assemblyName, TimeSpan duration)
        {
            AssemblyName = assemblyName;
            Duration = duration;
        }

        public override string ToString() => AssemblyName;
    }
}

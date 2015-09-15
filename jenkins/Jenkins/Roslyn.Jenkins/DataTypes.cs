using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JobInfo
    {
        public readonly UniqueJobId JobId;
        public readonly PullRequestInfo PullRequestInfo;
        public readonly JobState State;

        public JobInfo(UniqueJobId jobId, PullRequestInfo pullRequestInfo, JobState state)
        {
            JobId = jobId;
            PullRequestInfo = pullRequestInfo;
            State = state;
        }

        public override string ToString()
        {
            return $"{JobId.Id} {PullRequestInfo.PullUrl} {State}";
        }
    }

    public enum Platform
    {
        Windows,
        Linux,
        Mac,
    }

    public struct JobId
    {
        public readonly int Id;
        public readonly Platform Platform;

        public JobId(int id, Platform platform)
        {
            Id = id;
            Platform = platform;
        }

        public override string ToString()
        {
            return $"{Id} - {Platform}";
        }
    }

    /// <summary>
    /// A <see cref="JobId"/> is a non-unique structure since Jenkins recycles ids on a 
    /// periodic basis.  This structure represents a globally unique job id by appending
    /// a <see cref="DateTime"/> value. 
    /// </summary>
    public struct UniqueJobId
    {
        public JobId JobId {get; }
        public DateTime Date { get; }
        public int Id => JobId.Id;
        public Platform Platform => JobId.Platform;
        public string Key => $"{Date}_{Id}_{Platform}";

        public UniqueJobId(int id, Platform platform, DateTime date)
        {
            JobId = new JobId(id, platform);
            Date = date.Date;
        }

        public UniqueJobId(JobId jobId, DateTime date)
            : this(jobId.Id, jobId.Platform, date)
        {

        }

        public static UniqueJobId? TryParse(string key)
        {
            var items = key.Split('_');
            if (items.Length != 3)
            {
                return null;
            }

            DateTime date = DateTime.Now;
            Platform platform = Platform.Windows;
            int id = 0;
            if (!DateTime.TryParse(items[0], out date) ||
                !int.TryParse(items[1], out id) ||
                !Enum.TryParse<Platform>(items[2], out platform))
            {
                return null;
            }

            return new UniqueJobId(id, platform, date);
        }

        public override string ToString()
        {
            return $"{JobId} - {Date}";
        }
    }

    public struct PullRequestInfo
    {
        public readonly string AuthorEmail;
        public readonly int Id;
        public readonly string PullUrl;
        public readonly string Sha1;

        public PullRequestInfo(string authorEmail, int id, string pullUrl, string sha1)
        {
            AuthorEmail = authorEmail;
            Id = id;
            PullUrl = pullUrl;
            Sha1 = sha1;
        }

        public override string ToString()
        {
            return $"{PullUrl} - {AuthorEmail}";
        }
    }

    /// <summary>
    /// The key which uniquely identifies a test asset.  Anytime this appears more than once
    /// in the set of job infos then the same set of changes were run twice through Jenkins
    /// </summary>
    public struct BuildKey
    {
        public readonly int PullId;
        public readonly string Sha1;

        public BuildKey(int pullId, string sha1)
        {
            PullId = pullId;
            Sha1 = sha1;
        }

        public override string ToString()
        {
            return $"{PullId} - {Sha1}";
        }
    }

    public enum JobState
    {
        Succeeded,
        Failed,
        Aborted,
        Running,
    }

    public sealed class JobResult
    {
        private readonly JobFailureInfo _failureInfo;

        public readonly JobId Id;
        public readonly JobState State;

        public bool Succeeded => State == JobState.Succeeded;
        public bool Failed => State == JobState.Failed;
        public bool Running => State == JobState.Running;
        public bool Aborted => State == JobState.Aborted;

        public JobFailureInfo FailureInfo
        {
            get
            {
                if (!Failed)
                {
                    throw new InvalidOperationException();
                }

                return _failureInfo;
            }
        }

        public JobResult(JobId id, JobState state)
        {
            Debug.Assert(state != JobState.Failed);
            Id = id;
            State = state;
        }

        public JobResult(JobId id, JobFailureInfo failureInfo)
        {
            Id = id;
            State = JobState.Failed;
            _failureInfo = failureInfo;
        }
    }

    public enum JobFailureReason
    {
        Unknown,
        TestCase,
        Build,
        NuGet,
        Infrastructure,
    }

    public sealed class JobFailureInfo
    {
        public static readonly JobFailureInfo Unknown = new JobFailureInfo(JobFailureReason.Unknown);

        public JobFailureReason Reason;
        public List<string> Messages;

        public JobFailureInfo(JobFailureReason reason, List<string> messages = null)
        {
            Reason = reason;
            Messages = messages ?? new List<string>();
        }
    }
}

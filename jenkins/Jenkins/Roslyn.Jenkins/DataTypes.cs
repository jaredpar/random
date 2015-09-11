﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Jenkins
{
    public sealed class JobInfo
    {
        public readonly JobId BuildId;
        public readonly PullRequestInfo PullRequestInfo;

        public JobInfo(JobId buildId, PullRequestInfo pullRequestInfo)
        {
            BuildId = buildId;
            PullRequestInfo = pullRequestInfo;
        }

        public override string ToString()
        {
            return $"{BuildId.Id} {PullRequestInfo.PullUrl}";
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

    public sealed class JobResult
    {
        private readonly JobFailureInfo _failureInfo;

        public readonly JobId Id;
        public bool Succeeded;

        public JobFailureInfo FailureInfo
        {
            get
            {
                if (Succeeded)
                {
                    throw new InvalidOperationException();
                }

                return _failureInfo;
            }
        }

        public JobResult(JobId id)
        {
            Id = id;
            Succeeded = true;
        }

        public JobResult(JobId id, JobFailureInfo failureInfo)
        {
            Id = id;
            Succeeded = false;
            _failureInfo = failureInfo;
        }
    }

    public enum JobFailureReason
    {
        Unknown,
        TestCase,
        Build,
    }

    public sealed class JobFailureInfo
    {
        public static readonly JobFailureInfo Unknown = new JobFailureInfo(JobFailureReason.Unknown);

        public JobFailureReason Reason;
        public List<string> FailedTestList;

        private JobFailureInfo(JobFailureReason reason, List<string> failedTestList = null)
        {
            Reason = reason;
            FailedTestList = failedTestList ?? new List<string>();
        }

        public static JobFailureInfo TestFailed(List<string> failedTestList)
        {
            return new JobFailureInfo(JobFailureReason.TestCase, failedTestList);
        }
    }
}

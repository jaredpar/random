using System;
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
}

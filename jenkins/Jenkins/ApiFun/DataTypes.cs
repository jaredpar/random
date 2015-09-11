using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFun
{
    internal sealed class JobInfo
    {
        public readonly JobId BuildId;
        public readonly PullRequestInfo PullRequestInfo;

        internal JobInfo(JobId buildId, PullRequestInfo pullRequestInfo)
        {
            BuildId = buildId;
            PullRequestInfo = pullRequestInfo;
        }

        public override string ToString()
        {
            return $"{BuildId.Id} {PullRequestInfo.PullUrl}";
        }
    }

    internal enum Platform
    {
        Windows,
        Linux,
        Mac,
    }

    internal struct JobId
    {
        internal readonly int Id;
        internal readonly Platform Platform;

        internal JobId(int id, Platform platform)
        {
            Id = id;
            Platform = platform;
        }

        public override string ToString()
        {
            return $"{Id} - {Platform}";
        }
    }

    internal struct PullRequestInfo
    {
        internal readonly string AuthorEmail;
        internal readonly int Id;
        internal readonly string PullUrl;
        internal readonly string Sha1;

        internal PullRequestInfo(string authorEmail, int id, string pullUrl, string sha1)
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
    internal struct BuildKey
    {
        internal readonly int PullId;
        internal readonly string Sha1;

        internal BuildKey(int pullId, string sha1)
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

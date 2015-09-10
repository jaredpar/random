using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFun
{
    internal sealed class JobInfo
    {
        public readonly Build Build;
        public readonly PullRequestInfo PullRequestInfo;

        internal JobInfo(Build build, PullRequestInfo pullRequestInfo)
        {
            Build = build;
            PullRequestInfo = pullRequestInfo;
        }

        public override string ToString()
        {
            return $"{Build.Number} {PullRequestInfo.PullUrl}";
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
}

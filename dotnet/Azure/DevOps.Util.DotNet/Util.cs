using DevOps.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace DevOps.Util.DotNet
{
    public static class Util
    {
        /// <summary>
        /// Normalize the branch name so that has the short human readable form of the branch
        /// name
        /// </summary>
        public static string NormalizeBranchName(string fullName) => BranchName.Parse(fullName).ShortName;

        public static string GetOrganization(Build build)
        {
            var uri = new Uri(build.Url);
            return uri.PathAndQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        /// <summary>
        /// Get a human readable build URI for the build
        /// </summary>
        /// <param name="build"></param>
        /// <returns></returns>
        public static Uri GetUri(Build build)
        {
            var organization = GetOrganization(build);
            var uri = $"https://dev.azure.com/{organization}/{build.Project.Name}/_build/results?buildId={build.Id}";
            return new Uri(uri);
        }

        public static async Task DoWithTransactionAsync(SqlConnection connection, string transactionName, Func<SqlTransaction, Task> process)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var transaction = connection.BeginTransaction(transactionName);

            try
            {
                await process(transaction);
                transaction.Commit();
            }
            catch (Exception)
            {
                // Attempt to roll back the transaction. 
                try
                {
                    transaction.Rollback();
                }
                catch (Exception)
                {
                    // Expected that this will fail if the transaction fails on the server
                }
            }
        }
    }

    public readonly struct BranchName
    {
        public string FullName { get; }
        public string ShortName { get; }
        public bool IsPullRequest { get; }

        private BranchName(string fullName, string shortName, bool isPullRequest)
        {
            FullName = fullName;
            ShortName = shortName;
            IsPullRequest = isPullRequest;
        }

        public static bool TryParse(string fullName, out BranchName branchName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                branchName = default;
                return false;
            }

            if (fullName[0] == '/')
            {
                fullName = fullName.Substring(1);
            }

            var normalPrefix = "refs/heads/";
            var prPrefix = "refs/pull/";
            string shortName;
            bool isPullRequest;
            if (fullName.StartsWith(normalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                shortName = fullName.Substring(normalPrefix.Length);
                isPullRequest = false;
            }
            else if (fullName.StartsWith(prPrefix, StringComparison.OrdinalIgnoreCase))
            {
                shortName = fullName.Split(new[] { '/' })[2];
                isPullRequest = true;
            }
            else
            {
                shortName = fullName;
                isPullRequest = false;
            }

            branchName = new BranchName(fullName, shortName, isPullRequest);
            return true;
        }

        public static BranchName Parse(string fullName)
        {
            if (!TryParse(fullName, out var branchName))
            {
                throw new Exception($"Invalid branch full name {fullName}");
            }

            return branchName;
        }

        public override string ToString() => FullName;
    }
}

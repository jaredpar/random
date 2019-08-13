using System;
using System.Collections.Generic;
using System.Text;

namespace DevOpsFun
{
    public static class Util
    {
        /// <summary>
        /// Normalize the branch name so that has the short human readable form of the branch
        /// name
        /// </summary>
        public static string NormalizeBranchName(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
            {
                return branchName;
            }

            if (branchName[0] == '/')
            {
                branchName = branchName.Substring(1);
            }

            var prefix = "refs/heads/";
            if (branchName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                branchName = branchName.Substring(prefix.Length);
            }

            return branchName;
        }
    }
}

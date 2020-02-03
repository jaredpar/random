
using DevOps.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace DevOps.Util.DotNet
{
    public static class HelixUtil
    {
        public static bool IsHelixTestCaseResult(TestCaseResult testCaseResult) => TryGetHelixJobId(testCaseResult, out var _);

        public static bool TryGetHelixJobId(TestCaseResult testCaseResult, out string helixJobId)
        {
            helixJobId = null;
            try
            {
                if (testCaseResult.Comment is null)
                {
                    return false;
                }

                dynamic obj = JObject.Parse(testCaseResult.Comment);
                helixJobId = (string)obj.HelixJobId;
                return !string.IsNullOrEmpty(helixJobId);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<MemoryStream> GetHelixAttachmentContentAsync(
            DevOpsServer server,
            string project,
            int runId,
            int testCaseResultId)
        {
            var attachments = await server.GetTestCaseResultAttachmentsAsync(project, runId, testCaseResultId);
            var attachment = attachments.FirstOrDefault(x => x.FileName == "UploadFileResults.txt");
            if (attachment is null)
            {
                return null;
            }

            return await server.DownloadTestCaseResultAttachmentZipAsync(project, runId, testCaseResultId, attachment.Id);
        }

        /// <summary>
        /// Parse out the UploadFileResults file to get the console and core URIs
        /// </summary>
        public static async Task<(string ConsoleUri, string CoreUri)> GetHelixDataUris(Stream resultsStream)
        {
            string consoleUri = null;
            string coreUri = null;

            using var reader = new StreamReader(resultsStream);
            string line = await reader.ReadLineAsync();
            while (line is object)
            {
                if (Regex.IsMatch(line, @"console.*\.log:"))
                {
                    consoleUri = (await reader.ReadLineAsync()).Trim();
                }
                else if (Regex.IsMatch(line, @"core\.:"))
                {
                    coreUri = (await reader.ReadLineAsync()).Trim();
                }

                line = await reader.ReadLineAsync();
            }

            return (consoleUri, coreUri);
        }

        public static async Task<(string ConsoleUri, string CoreUri)> GetHelixDataUris(
            DevOpsServer server,
            string project,
            int runId,
            int testCaseResultId)
        {
            using var stream = await GetHelixAttachmentContentAsync(server, project, runId, testCaseResultId);
            if (stream is null)
            {
                return (null, null);
            }

            return await GetHelixDataUris(stream);
        }
    }
}
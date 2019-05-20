using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

/*
namespace pulllogs
{
    public struct AzdoDetails
    {
        public long BuildId;
        public string Repository;
        public string Author;
        public string PrNumber;
        public string BuildNumber;
        public string Status;
        public string Result;
        public List<string> ValidationResults;
        public string Url;
        public string Queued;
        public string Started;
        public string Finished;
    }

    public struct ChangeLog
    {
        public string Author;
        public string Commit;
    }

    public class AzDo
    {
        public AzDo(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new Exception("Must provide a valid Azure Dev Ops access token");
            AccessToken = token;
        }

        public List<AzdoDetails> GetAllBuilds(int definitionId, bool isPublic, int limit)
        {
            var json = default(Utf8JsonReader);
            var url = String.Format(AzDoListUrl, isPublic ? AzDoPublic : AzDoInternal, definitionId) + "&$top=" + limit;

            if (!GetAzDoJsonReader(url, out json))
            {
                throw new Exception("Failed to get logs for build definition " + definitionId);
            }

            // get all the buildlogs
            var results = new List<AzdoDetails>();
            AzdoDetails details;
            while (ParseAzdoBuildLog(ref json, out details))
            {
                results.Add(details);
            }

            if (results.Count == 0 && details.BuildId > 0) throw new Exception("There were no results but there was a partial pull");

            return results;
        }

        public AzdoDetails GetBuildLogUrl(string azdoBuildId, out bool isPublic)
        {
            Utf8JsonReader json = default(Utf8JsonReader);
            var url = String.Format(AzDoBuildUrl, AzDoInternal, azdoBuildId);
            isPublic = false;

            // attempt to access the log
            var secondAttemp = false;
            do
            {
                // try with this combination
                try
                {
                    if (GetAzDoJsonReader(url, out json)) break;
                }
                catch (System.Net.WebException)
                {
                    // retry
                }

                // try again as public
                url = String.Format(AzDoBuildUrl, AzDoPublic, azdoBuildId);
                isPublic = true;

                if (secondAttemp) throw new Exception("Unable to retrieve GetBuildLogLists");
                secondAttemp = true;
            }
            while (true);

            // there is only one
            if (!ParseAzdoBuildLog(ref json, out AzdoDetails details)) throw new Exception("Failed to parse build logs");

            return details;
        }

        public List<ChangeLog> GetChangeLogs(string azdoBuildId, bool isPublic)
        {
            var results = new List<ChangeLog>();
            Utf8JsonReader json = default(Utf8JsonReader);
            var url = String.Format(AzDoChangeUrl, isPublic ? AzDoPublic : AzDoInternal, azdoBuildId);

            try
            {
                if (!GetAzDoJsonReader(url, out json)) throw new Exception("Unable to retrieve GetChangeLog");
            }
            catch (System.Net.WebException e)
            {
                if (e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)) return results;
                else throw e;
            }

            var displayName = "";
            var location = "";
            var isDisplayName = false;
            var isLocation = false;
            while (json.Read())
            {
                // {
                //  "id": "d44e0bb6db768153bf4f67a9420e59e397494e9e",
                // "message": "Update DefaultInterfacesTests.cpp",
                // "type": "GitHub",
                // "author": {
                //   "displayName": "AaronRobinsonMSFT",
                //   "_links": { "avatar": { "href": "https://avatars0.githubusercontent.com/u/30635565?v=4" } },
                //   "id": "arobins@microsoft.com",
                // "imageUrl": "https://avatars0.githubusercontent.com/u/30635565?v=4"
                // },
                // "timestamp": "2019-04-13T18:58:05Z",
                // "location": "https://api.github.com/repos/dotnet/coreclr/commits/d44e0bb6db768153bf4f67a9420e59e397494e9e",
                // "displayUri": "https://github.com/dotnet/coreclr/commit/d44e0bb6db768153bf4f67a9420e59e397494e9e"
                // }
                isDisplayName |= json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_DisplayName);
                if (isDisplayName && json.TokenType == JsonTokenType.String)
                {
                    if (!string.IsNullOrWhiteSpace(displayName)) throw new Exception("Failed to properly parse json - found duplicate display names : " + displayName);
                    displayName = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                    isDisplayName = false;
                }

                isLocation |= json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Location);
                if (isLocation && json.TokenType == JsonTokenType.String)
                {
                    if (!string.IsNullOrWhiteSpace(location)) throw new Exception("Failed to properly parse json - found duplicate locations : " + location);
                    location = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                    isLocation = false;
                }

                if (!string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(location))
                {
                    // got a pair
                    results.Add(new ChangeLog()
                    {
                        Author = displayName,
                        Commit = location
                    });
                    displayName = location = "";
                }
            }

            return results;
        }

        public List<string> GetBuildLogs(string url)
        {
            var logs = new List<string>();
            Utf8JsonReader json = default(Utf8JsonReader);

            if (!GetAzDoJsonReader(url, out json))
            {
                throw new Exception("Failed to retrieve AzDo details about this URL");
            }

            var urlSeen = false;
            while (json.Read())
            {
                // {
                //  "lineCount": 27855,
                //  "createdOn": "2019-04-21T12:00:05.273Z",
                //  "lastChangedOn": "2019-04-21T12:00:05.313Z",
                //  "id": 1,
                //  "type": "Container",
                //  "url": "https://dnceng.visualstudio.com/7ea9116e-9fac-403d-b258-b31fcf1bb293/_apis/build/builds/163553/logs/1"
                // }

                urlSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Url));

                if (urlSeen && json.TokenType == JsonTokenType.String)
                {
                    // assume this is the url
                    logs.Add(System.Text.Encoding.UTF8.GetString(json.ValueSpan));
                    urlSeen = false;
                }
            }

            return logs;
        }

        public string[] GetText(string url)
        {
            var request = WebRequest.Create(url);

            var encoded = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(AccessToken));
            request.Headers.Add("Authorization", "Basic " + encoded);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var results = new List<string>();
                        while (!reader.EndOfStream)
                        {
                            results.Add(reader.ReadLine());
                        }
                        return results.ToArray();
                    }
                }
            }
        }

        #region private
        private const string AzDoListUrl = "https://dev.azure.com/dnceng/{0}/_apis/build/builds?definitions={1}&BuildQueryOrder=queueTimeDescending";
        private const string AzDoBuildUrl = "https://dev.azure.com/dnceng/{0}/_apis/build/builds/{1}";
        private const string AzDoLogUrl = "https://dev.azure.com/dnceng/{0}/_apis/build/builds/{1}/logs";
        private const string AzDoChangeUrl = "https://dev.azure.com/dnceng/{0}/_apis/build/builds/{1}/changes";
        private const string AzDoLatestUrl = "https://dev.azure.com/dnceng/{0}/_apis/build/latest/{1}";

        private const string AzDoPublic = "public";
        private const string AzDoInternal = "internal";

        private string AccessToken;

        // json element
        private static readonly byte[] s_DisplayName = System.Text.Encoding.UTF8.GetBytes("displayName");
        private static readonly byte[] s_Location = System.Text.Encoding.UTF8.GetBytes("location");
        private static readonly byte[] s_Logs = System.Text.Encoding.UTF8.GetBytes("logs");
        private static readonly byte[] s_Url = System.Text.Encoding.UTF8.GetBytes("url");
        private static readonly byte[] s_ValidationResuls = System.Text.Encoding.UTF8.GetBytes("validationResults");
        private static readonly byte[] s_PrNumber = System.Text.Encoding.UTF8.GetBytes("pr.number");
        private static readonly byte[] s_BuildNumber = System.Text.Encoding.UTF8.GetBytes("buildNumber");
        private static readonly byte[] s_Status = System.Text.Encoding.UTF8.GetBytes("status");
        private static readonly byte[] s_Result = System.Text.Encoding.UTF8.GetBytes("result");
        private static readonly byte[] s_Author = System.Text.Encoding.UTF8.GetBytes("pr.sender.name");
        private static readonly byte[] s_Message = System.Text.Encoding.UTF8.GetBytes("message");
        private static readonly byte[] s_Id = System.Text.Encoding.UTF8.GetBytes("id");
        private static readonly byte[] s_Repository = System.Text.Encoding.UTF8.GetBytes("repository");
        private static readonly byte[] s_Definition = System.Text.Encoding.UTF8.GetBytes("definition");
        private static readonly byte[] s_QueueTime = System.Text.Encoding.UTF8.GetBytes("queueTime");
        private static readonly byte[] s_StartTime = System.Text.Encoding.UTF8.GetBytes("startTime");
        private static readonly byte[] s_FinishTime = System.Text.Encoding.UTF8.GetBytes("finishTime");

        private bool ParseAzdoBuildLog(ref Utf8JsonReader json, out AzdoDetails result)
        {
            result = new AzdoDetails();

            var startingDepth = 0;
            var logSeen = false;
            var urlSeen = false;
            var authorSeen = false;
            var pnumberSeen = false;
            var bnumberSeen = false;
            var statusSeen = false;
            var resultSeen = false;
            var vresultSeen = false;
            var repositorySeen = false;
            var idSeen = false;
            var outerIdSeen = false;
            var qtimeSeen = false;
            var stimeSeen = false;
            var ftimeSeen = false;
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.EndObject && json.CurrentDepth < startingDepth) return true;

                //   "logs": {
                //      "id": 0,
                //      "type": "Container",
                //      "url": "https://dnceng.visualstudio.com/7ea9116e-9fac-403d-b258-b31fcf1bb293/_apis/build/builds/163553/logs"
                //   }

                // first id seen
                outerIdSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Id));
                if (result.BuildId == 0 && outerIdSeen && json.TokenType == JsonTokenType.Number)
                {
                    startingDepth = json.CurrentDepth;
                    if (!json.TryGetInt64(out result.BuildId)) throw new Exception("Failed to get the buildId");
                }

                // queue time
                qtimeSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_QueueTime));
                if (qtimeSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Queued = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                    qtimeSeen = false;
                }

                // start time
                stimeSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_StartTime));
                if (stimeSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Started = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                    stimeSeen = false;
                }

                // finish time
                ftimeSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_FinishTime));
                if (ftimeSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Finished = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                    ftimeSeen = false;
                }

                // retrieve the url
                logSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Logs));
                if (logSeen) urlSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Url));

                if (string.IsNullOrWhiteSpace(result.Url) && logSeen && urlSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Url = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                }

                // get Author
                authorSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Author));
                if (string.IsNullOrWhiteSpace(result.Author) && authorSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Author = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                }

                // get PrNumber
                pnumberSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_PrNumber));
                if (string.IsNullOrWhiteSpace(result.PrNumber) && pnumberSeen && json.TokenType == JsonTokenType.String)
                {
                    result.PrNumber = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                }

                // get BuildNumber
                bnumberSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_BuildNumber));
                if (string.IsNullOrWhiteSpace(result.BuildNumber) && bnumberSeen && json.TokenType == JsonTokenType.String)
                {
                    result.BuildNumber = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                }

                // get Status
                statusSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Status));
                if (string.IsNullOrWhiteSpace(result.Status) && statusSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Status = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                }

                // get Result
                resultSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Result));
                if (string.IsNullOrWhiteSpace(result.Result) && resultSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Result = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                }

                // get ValidationResults
                vresultSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_ValidationResuls));
                if (vresultSeen && json.TokenType == JsonTokenType.StartArray)
                {

                    var messageSeen = false;
                    while (json.Read() && json.TokenType != JsonTokenType.EndArray)
                    {
                        messageSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Message));
                        if (messageSeen && json.TokenType == JsonTokenType.String)
                        {
                            if (result.ValidationResults == null) result.ValidationResults = new List<string>();
                            result.ValidationResults.Add(System.Text.Encoding.UTF8.GetString(json.ValueSpan));
                            messageSeen = false;
                        }
                    }
                }

                // respository
                repositorySeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Repository));
                if (repositorySeen) idSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Id));
                if (repositorySeen && idSeen && json.TokenType == JsonTokenType.String)
                {
                    result.Repository = System.Text.Encoding.UTF8.GetString(json.ValueSpan);
                    idSeen = false;
                }
            }

            return false;
        }

        private string GetAzDoText(string url)
        {
            var retries = 10;
            while (--retries > 0)
            {
                try
                {
                    var request = WebRequest.Create(url);

                    var encoded = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(AccessToken));
                    request.Headers.Add("Authorization", "Basic " + encoded);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (WebException e)
                {
                    // not found happens when we check internal vs public - do not display the error
                    if (!e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)) Console.WriteLine(e);
                    // service unavailable means that AzDo is down, if not fail
                    if (!e.Message.Contains("service unavailable", StringComparison.OrdinalIgnoreCase)) throw e;
                }

                // pause
                System.Threading.Thread.Sleep(5000);
            }

            // throw if not successful
            throw new Exception("Failed to retrieve content from " + url);
        }

        private bool IsAzDoFailure(string text)
        {
            // {"$id":"1","innerException":null,"message":"The requested build 139106 could not be found.","typeName":"Microsoft.TeamFoundation.Build.WebApi.BuildNotFoundException, Microsoft.TeamFoundation.Build2.WebApi","typeKey":"BuildNotFoundException","errorCode":0,"eventId":3000}
            return text.Contains("Microsoft.TeamFoundation.Build.WebApi.BuildNotFoundException", StringComparison.OrdinalIgnoreCase);
        }

        private bool GetAzDoJsonReader(string url, out Utf8JsonReader reader)
        {
            var text = GetAzDoText(url);

            // check if valid json
            if (string.IsNullOrWhiteSpace(text) || (text.Length > 0 && text[0] != '{'))
            {
                reader = default(Utf8JsonReader);
                return false;
            }

            // check if this is a failure case
            if (IsAzDoFailure(text))
            {
                reader = default(Utf8JsonReader);
                return false;
            }


            // return the json reader
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            reader = new Utf8JsonReader(bytes, true, default(JsonReaderState));

            return true;
        }

        private int GetAzdoDefinitionId(string definition, out bool isPublic)
        {
            // this method is having an authentication issue, and needs to be investigated

            Utf8JsonReader json = default(Utf8JsonReader);
            var url = String.Format(AzDoLatestUrl, AzDoInternal, definition);
            isPublic = false;

            // attempt to access the log
            var secondAttemp = false;
            do
            {
                // try with this combination
                try
                {
                    if (GetAzDoJsonReader(url, out json)) break;
                }
                catch (System.Net.WebException e)
                {
                    // retry
                }

                // try again as public
                url = String.Format(AzDoLatestUrl, AzDoPublic, definition);
                isPublic = true;

                if (secondAttemp) throw new Exception("Unable to retrieve GetBuildLogLists");
                secondAttemp = true;
            }
            while (true);

            var definitionSeen = false;
            var idSeen = false;
            while (json.Read())
            {
                definitionSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Definition));
                if (definitionSeen) idSeen |= (json.TokenType == JsonTokenType.PropertyName && json.ValueSpan.SequenceEqual(s_Id));

                if (definitionSeen && idSeen && json.TokenType == JsonTokenType.String)
                {
                    return Convert.ToInt32(System.Text.Encoding.UTF8.GetString(json.ValueSpan));
                }
            }

            throw new Exception("Failed to retrieve an id for " + definition);
        }
        #endregion
    }
}
*/

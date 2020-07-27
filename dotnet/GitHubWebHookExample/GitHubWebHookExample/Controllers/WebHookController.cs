using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace GitHubWebHookExample.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class WebHookController : ControllerBase
    {
        public class Info
        {
            public string Action { get; set; }
            public string Body { get; set; }
            public string EventName { get; set; }
            public string SignatureWithPrefix { get; set; }
            public bool? SignatureVerified { get; set; }
        }

        private const string Secret = "this-is-not-really-a-secret";
        private static readonly List<Info> s_infoList = new List<Info>();

        [HttpPost]
        public async Task Hook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var info = new Info();
            info.Body = body;

            dynamic d = JObject.Parse(body);
            info.Action = d.action;

            const string shaPrefix = "sha1=";
            Request.Headers.TryGetValue("X-GitHub-Event", out StringValues eventName);
            Request.Headers.TryGetValue("X-Hub-Signature", out StringValues signatureWithPrefixValues);
            Request.Headers.TryGetValue("X-GitHub-Delivery", out StringValues delivery);

            info.EventName = eventName;
            string signatureWithPrefix = signatureWithPrefixValues;
            if (!string.IsNullOrEmpty(signatureWithPrefix) &&
                signatureWithPrefix.StartsWith(shaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                info.SignatureWithPrefix = signatureWithPrefix;
                var signature = signatureWithPrefix.Substring(shaPrefix.Length);
                var payloadBytes = Encoding.ASCII.GetBytes(body);

                using var hmSha1 = new HMACSHA1(Encoding.ASCII.GetBytes(Secret));
                var hash = hmSha1.ComputeHash(payloadBytes);
                var hashString = ToHexString(hash);
                info.SignatureVerified = hashString.Equals(signature);

                static string ToHexString(byte[] bytes)
                {
                    var builder = new StringBuilder(bytes.Length * 2);
                    foreach (byte b in bytes)
                    {
                        builder.AppendFormat("{0:x2}", b);
                    }

                    return builder.ToString();
                }
            }

            s_infoList.Add(info);
        }

        [HttpGet]
        public string Get()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < s_infoList.Count; i++)
            {
                var info = s_infoList[i];
                builder.AppendLine(i.ToString());
                builder.AppendLine($"SignatureWithPrefix: {info.SignatureWithPrefix}");
                builder.AppendLine($"SignatureVerified: {info.SignatureVerified}");
                builder.AppendLine($"EventName: {info.EventName}");
                builder.AppendLine($"Action: {info.Action}");
                builder.AppendLine($"Body: {info.Body}");
            }

            return builder.ToString();
        }
    }
}

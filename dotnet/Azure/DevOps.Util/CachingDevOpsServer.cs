
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DevOps.Util
{
    public sealed class CachingDevOpsServer
    {
        private SHA256 Hash { get; } = SHA256.Create();

        public DevOpsServer Server { get; }

        public CachingDevOpsServer(DevOpsServer server)
        {
            Server = server;
        }

        private string GetName(params string[] inputs)
        {

        }
    }
}
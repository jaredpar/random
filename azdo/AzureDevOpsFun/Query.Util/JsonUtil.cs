using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Query.Util
{
    internal static class JsonUtil
    {
        internal static BuildData CreateBuildData(JObject obj)
        {
            var id = (int)obj["id"];
            var buildNumber = (string)obj["buildNumber"];
            var buildUri = (string)obj["uri"];
            var status = (string)obj["status"];
            return new BuildData(id, buildNumber, buildUri, status);
        }
    }
}

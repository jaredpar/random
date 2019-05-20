using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Query.Util
{
    public sealed class BuildData
    {
        public int Id { get; set; }
        public string BuildNumber { get; set; }
        [JsonProperty(PropertyName = "uri")]
        public string BuildUri { get; set; }
        public string Status { get; set; }

        public override string ToString() => $"Id: {Id} BuildNumber: {BuildNumber}";
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/builds/get%20build%20logs?view=azure-devops-rest-5.0#buildlog
    /// </summary>
    public sealed class BuildLog
    {
        public int Id { get; set; }
        public int LineCount { get; set; }
        public string Type { get; set; }

        public override string ToString() => $"Id: {Id} Type: {Type} LineCount: {LineCount}";
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Query.Util
{
    public sealed class BuildData
    {
        public int Id { get; }
        public string BuildNumber { get; }
        public string Status { get; }

        public BuildData(
            int id,
            string buildNumber,
            string status)
        {
            Id = id;
            BuildNumber = buildNumber;
            Status = status;
        }
    }
}

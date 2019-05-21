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

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#timeline
    /// </summary>
    public sealed class Timeline
    {
        public string Id { get; set; }
        public int ChangeId { get; set; }
        public string LastChangedBy { get; set; }
        public string LastChangedOn { get; set; }
        public TimelineRecord[] Records { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#timelinerecord
    /// </summary>
    public sealed class TimelineRecord
    {
        public int Attempt { get; set; }
        public int ChangeId { get; set; }
        public string CurrentOperation { get; set; }
        public TimelineReference Details { get; set; }
        public int ErrorCount { get; set; }
        public string FinishTime { get; set; }
        public string Id { get; set; }
        public Issue[] Issues { get; set; }
        public string LastModified { get; set; }
        public string Name { get; set; }
        public BuildLogReference Log { get; set; }
        public int Order { get; set; }
        public string ParentId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int PercentComplete { get; set; }
        public TimelineAttempt[] PreviousAttempts { get; set; }
        public TaskResult Result { get; set; }
        public string ResultCode { get; set; }
        public string StartTime { get; set; }
        public TaskReference Task { get; set; }
        public string Url { get; set; }
        public int WarningCount { get; set; }
        public string WorkerName { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#buildlogreference
    /// </summary>
    public sealed class BuildLogReference
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#issue
    /// </summary>
    public sealed class Issue
    {
        public string Category { get; set; }
        public object Data { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#timelinereference
    /// </summary>
    public sealed class TimelineReference
    {
        public int ChangeId { get; set; }
        public string Id { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#timelineattempt
    /// </summary>
    public sealed class TimelineAttempt
    {
        public int Attempt { get; set; }
        public string RecordId { get; set; }
        public string TimelineId { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#taskresult
    /// </summary>
    public enum TaskResult
    {
        Abandoned,
        Canceled,
        Failed,
        Skipped,
        Succeeded,
        SucceededWithIssues
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#timelinerecordstate
    /// </summary>
    public enum TimelineRecordState
    {
        Completed,
        InProgress,
        Pending
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-5.0#taskreference
    /// </summary>
    public sealed class TaskReference
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }
}

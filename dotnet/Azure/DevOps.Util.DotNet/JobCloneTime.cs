using System;
using System.Collections.Generic;
using System.Text;

namespace DevOps.Util.DotNet
{
    public readonly struct JobCloneTime
    {
        public string JobName { get; }
        public TimeSpan Duration { get; }

        /// <summary>
        /// Size of the fetch operation specified in KiB
        /// </summary>
        public double? FetchSize { get; }

        /// <summary>
        /// Mix speed of the fetch operation specified in KiB
        /// </summary>
        public double? MinFetchSpeed { get; }

        /// <summary>
        /// Max speed of the fetch operation specifieed in KiB
        /// </summary>
        public double? MaxFetchSpeed { get; }

        public JobCloneTime(
            string jobName,
            TimeSpan duration,
            double? fetchSize = null,
            double? minFetchSpeed = null,
            double? maxFetchSpeed = null)
        {
            JobName = jobName;
            Duration = duration;
            FetchSize = fetchSize;
            MinFetchSpeed = minFetchSpeed;
            MaxFetchSpeed = maxFetchSpeed;
        }
    }
}

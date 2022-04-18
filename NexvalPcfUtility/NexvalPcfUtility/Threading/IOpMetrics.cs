using System;

namespace Nexval.Framework.PCF.Threading
{
    public interface IOpMetrics
    {
        /// <summary>
        /// The date-time in UTC, when service was started
        /// </summary>
        DateTime StartTimeUTC { get; }

        /// <summary>
        /// Total # of requests executed till now
        /// </summary>
        int RequestCount { get; }

        /// <summary>
        /// Total # of requests which were failed in execution
        /// </summary>
        int FailureCount { get; }

        /// <summary>
        /// Total # of successfully executed requests.
        /// </summary>
        int SuccessCount { get; }

        /// <summary>
        /// Operation Metrics - Success Ratio in percentage
        /// </summary>
        float SuccessRatioInPercent { get; }
    }

}

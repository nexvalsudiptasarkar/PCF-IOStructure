using System;

namespace Nexval.Framework.PCF.Threading
{
    //internal sealed class OpMetrics : IOpMetrics
    //{
    //    private readonly int _requestCount;
    //    private readonly int _failureCount;

    //    public OpMetrics(int requestCount, int failureCount)
    //    {
    //        _requestCount = requestCount;
    //        _failureCount = failureCount;
    //    }

    //    /// <summary>
    //    /// Total # of requests executed till now
    //    /// </summary>
    //    public int RequestCount
    //    {
    //        get
    //        {
    //            return _requestCount;
    //        }
    //    }

    //    /// <summary>
    //    /// Total # of requests which were failed in execution
    //    /// </summary>
    //    public int FailureCount
    //    {
    //        get
    //        {
    //            return _failureCount;
    //        }
    //    }

    //    /// <summary>
    //    /// Operation Metrics - Success Ratio in percentage
    //    /// </summary>
    //    public float SuccessRatioInPercent
    //    {
    //        get
    //        {
    //            float successRate = 0.0f;
    //            if (_requestCount > 0)
    //            {
    //                successRate = 100.0f * ((float)(_requestCount - _failureCount) / (float)_requestCount);
    //            }
    //            return successRate;
    //        }
    //    }

    //    /// <summary>
    //    /// Operation Metrics - Success Ratio in percentage
    //    /// </summary>
    //    public int SuccessCount
    //    {
    //        get
    //        {
    //            return _requestCount - _failureCount;
    //        }
    //    }
    //}
}

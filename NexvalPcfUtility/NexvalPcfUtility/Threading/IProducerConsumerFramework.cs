using System;
using System.Threading;

namespace Nexval.Framework.PCF.Threading
{
    /// <summary>
    /// Raised when Producer Consumer Framework is Stopped
    /// </summary>
    /// <typeparam name="T">Data Type of the request served by the PCF Instance</typeparam>
    /// <param name="source">Producer Consumer Framework instance which has been stopped</param>
    public delegate void ProducerConsumerFrameworkStopped<T>(IProducerConsumerFramework<T> source);

    /// <summary>
    /// Raised when one or multiple Slots becomes available for scheduling more tasks.
    /// </summary>
    /// <typeparam name="T">Data Type of the request served by the PCF Instance</typeparam>
    /// <param name="expectedCountOfRequests">Expected count of requests with respect to available slots for execution</param>
    /// <param name="source">Producer Consumer Framework instance which has been stopped</param>
    /// <param name="continuePcfExecution">The event handler should set this to 'False' if PCF needs to terminated immediately</param>
    /// <returns>Requests for scheduling & processing ; NULL if no more request exists for scheduling & processing;</returns>
    /// <remarks>
    /// PCF will stop automatically upon getting no request (NULL) when 'IProducerConsumerFramework<T>' property 'AutoStopWhenNoNewRequest' is set to 'True'.
    /// </remarks>
    public delegate T[] PcfRequestFetcher<T>(int expectedCountOfRequests, IProducerConsumerFramework<T> source, ref bool continuePcfExecution);


    /// <summary>
    /// Fetches request in one thread & processes them in async manner in N# of threads, to generate result following Producer-Consumer Pattern.
    /// Following features are available:
    ///     Specify a pre-condition task or start-up task. The PCF will only start after successful execution of this delegate. 
    ///     This is useful for Windows Services designed to run in AWS & register for Auto-Scale-In/Out at the time of starting.
    ///     The Max # of threads should be set at the time of creation. Threads will be created/taken from thread pool. And these will be released when # of requests go below the Max # of threads set.
    ///     Can be customized to stop automatically when no more requests present for execution. By default this feature is turned off.
    /// </summary>
    /// <typeparam name="T">Data Type of the request served by the 'IProducerConsumerFramework' Instance</typeparam>
    public interface IProducerConsumerFramework<T> : IDisposable, IOpMetrics
    {
        /// <summary>
        /// Wait for Completion of all tasks upon calling of Stop method
        /// </summary>
        void WaitForCompletion();

        /// <summary>
        /// WaitHandle for caller to wait till Producer Consumer Framework stops processing
        /// </summary>
        WaitHandle WaitHandle { get; }

        /// <summary>
        /// Starts Producer-Consumer Process
        /// </summary>
        /// <returns>true if successful; else false, when it's already running</returns>
        bool Start();

        /// <summary>
        /// Stops Producer-Consumer Process
        /// </summary>
        void Stop();

        /// <summary>
        /// Checks if this Instance is running
        /// </summary>
        bool IsStopped { get; }

        /// <summary>
        /// Dumps Internal State Info to Application Log
        /// </summary>
        void LogInternalState();

        /// <summary>
        /// Control logging of Operation Metrics Info
        /// </summary>
        bool EnableOperationMetricsLogging { get; set; }

        /// <summary>
        /// Max # of Tasks to execute concurrently.
        /// </summary>
        int ConcurrencyThreshold { get; }

        /// <summary>
        /// Count of Active Tasks.
        /// </summary>
        int ActiveTaskCount { get; }

        /// <summary>
        /// Count of Available Slots for Concurrent Execution of Tasks.
        /// </summary>
        int AvailableSlots { get; }

        /// <summary>
        /// Checks if there is any request pending for processing.
        /// </summary>
        bool IsIdle { get; }

        /// <summary>
        /// Automatically Stops when there is no pending request & no further requests returned by client code. 
        /// </summary>
        bool AutoStopWhenNoNewRequest { get; }

        /// <summary>
        /// Checks if Operation Metrics Logging Enabled. 
        /// </summary>
        bool IsOperationMetricsLoggingEnabled { get; }

        /// <summary>
        /// Raised when a PCF is stopped
        /// </summary>
        event ProducerConsumerFrameworkStopped<T> OnStopped;
    }
}

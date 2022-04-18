using System;
using System.Threading;

namespace Nexval.Framework.PCF.Threading
{
    /// <summary>
    /// Raised when a Task Executed successfully
    /// </summary>
    /// <typeparam name="T">Any object as input to a Task</typeparam>
    /// <param name="source">The source ITaskManager object which raised this issue<T> </param>
    /// <param name="requestTrackingId">Request Tracking Id of the task which executed successfully. This Id was returned as a result of Enqueue to ITaskManager<T> </param>
    /// <param name="request">The input for the Task Executed</param>
    public delegate void TaskExecSuccessful<T>(ITaskManager<T> source, long requestTrackingId, T request);

    /// <summary>
    /// Raised when a Task Execution Failed
    /// </summary>
    /// <typeparam name="T">Any object as input to a Task</typeparam>
    /// <param name="source">The source ITaskManager object which raised this issue<T> </param>
    /// <param name="requestTrackingId">Request Tracking Id of the task which has failed to execute. This Id was returned as a result of Enqueue to ITaskManager<T> </param>
    /// <param name="request">The input for the Task Executed</param>
    /// <param name="retryCount">Number of times the task was executed</param>
    /// <param name="isPermanentlyFailed">Marks if the request execution is Permanently Failed, after exhausting the permitted # of retries</param>
    /// <param name="shouldTaskManagerBeTerminated">Should Task Manager execution be terminated after this? Set true to terminate execution; else false</param>
    public delegate void TaskExecFailed<T>(ITaskManager<T> source, long requestTrackingId, T request, int retryCount, bool isPermanentlyFailed, ref bool shouldTaskManagerBeTerminated);

    /// <summary>
    /// Raised when Task Manager is Stopped
    /// </summary>
    /// <typeparam name="T">Data Type of the request served by the Task Manager Instance</typeparam>
    /// <param name="source">Task Manager instance which has been stopped</param>
    public delegate void TaskManagerStopped<T>(ITaskManager<T> source);

    /// <summary>
    /// Creates 'ITaskManager' instance 
    /// 'ITaskManager' maintains an queue of requests (user defined) & executes/processes the requests concurrently.
    /// The Max # of threads should be set at the time of creation. 
    /// Threads will be created/taken from thread pool. And these will be released when # of requests go below the Max # of threads set. 
    /// </summary>
    public interface ITaskManager<T> : IDisposable
    {
        /// <summary>
        /// Starts Task Manager
        /// </summary>
        /// <returns>true if successful; else false</returns>
        bool Start();

        /// <summary>
        /// Stops Task Manager
        /// </summary>
        /// <returns>true if successful; else false</returns>
        bool Stop();

        /// <summary>
        /// Checks if this 'ITaskManager' Instance is running
        /// </summary>
        bool IsStopped { get; }

        /// <summary>
        /// Enqueues Multiple requests for Asynchronous execution. Returns immediately after putting requests in queue.
        /// </summary>
        /// <param name="requests">Requests to execute</param>
        /// <returns>The Request Ids for clients to track the execution of the requests; returns null for failure</returns>
        long[] EnqueueMultiple(params T[] requests);

        /// <summary>
        /// Enqueues request for Asynchronous execution. Returns immediately after putting request in queue.
        /// </summary>
        /// <param name="request">Request to execute</param>
        /// <returns>The Request Id for clients to track the execution of the request; returns -VE value for failure</returns>
        long Enqueue(T request);

        /// <summary>
        /// Operation Metrics (Succes Rate, # of requests processed etc.)
        /// </summary>
        IOpMetrics OperationMetrics { get; }

        /// <summary>
        /// Raised when a Task Executed successfully
        /// </summary>
        event TaskExecSuccessful<T> OnTaskExecSuccessful;

        /// <summary>
        /// Raised when a Task Execution Failed
        /// </summary>
        event TaskExecFailed<T> OnTaskExecFailed;

        /// <summary>
        /// Raised when Task Manager is stopped
        /// </summary>
        event TaskManagerStopped<T> OnStopped;

        /// <summary>
        /// WaitHandle for caller to wait till Task Manager stops processing
        /// Only useful when Task Manager supposed to Auto-Stop upon when there is no pending request 
        /// </summary>
        WaitHandle WaitHandle { get; }
    }
}

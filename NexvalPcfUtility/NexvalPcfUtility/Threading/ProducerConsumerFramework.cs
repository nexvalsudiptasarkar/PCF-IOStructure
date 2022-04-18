using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Nexval.Framework.PCF.Threading
{
    /// <summary>
    /// Implements Producer-Consumer Pattern to fetch request for processing & generate result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class ProducerConsumerFramework<T> : Disposable, IProducerConsumerFramework<T>
    {
        #region Member Variables
        private const int _timeOutForBlockingCollectionOpInMS = 250;
        private const int _waitTimeBeforeFetchingNextRequest = 1000;
        private const int _waitTimeBeforeTakingNextRequest = 1000;
        private const int _retryIntervalForPreConditionTaskInMS = 1000 * 30 * 1;//30s Interval
#if DEBUG
        private const int _frequencyOfShowingOpMetricsInSeconds = 45;//Every 45th second
#else
        private const int _frequencyOfShowingOpMetricsInSeconds = 120;//Every 2 Minutes
#endif

        private readonly bool _stopAutomaticallyIfNoNewRequestFetched;
        private readonly string _friendlyNameOfThisInstance;
        private CancellationTokenSource _cancellationTokenForStartUpJob;
        private Task _initializationTask;
        private CancellationTokenSource _cancellationTokenForManager;
        private bool _isPreConditionTaskExecSuccessful;
        private readonly Func<bool> _preConditionTaskBeforeInitialization;
        private readonly PcfRequestFetcher<T> _requestFetcher;
        private readonly Func<T, bool> _requestExecutor;
        private readonly int _concurrencyThreshold;
        private BlockingCollection<T> _requests;
        private ConcurrentDictionary<int, Task> _allTasks;
        private int _totalCountOfRequestsExecuted;
        private int _totalCountOfFailedExecution;
        private readonly object _lockForOpMetrics;
        private readonly Stopwatch _stopwatchForOperationMetricsLogging;
        private readonly Action _serviceStopper = null;
        private bool _isOperationMetricsLoggingEnabled;

        private ProducerConsumerFrameworkStopped<T> _onStopped;
        private readonly DateTime _startTimeUTC = DateTime.Now.ToUniversalTime();
        private AutoResetEvent _waitHandle = null;
        #endregion

        #region Constructor(s)
        public ProducerConsumerFramework(PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold)
            : this(null, requestFetcher, requestExecutor, concurrencyThreshold, null, null, false)
        {
        }

        public ProducerConsumerFramework(PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold, bool stopAutomaticallyIfNoNewRequestFetched)
            : this(null, requestFetcher, requestExecutor, concurrencyThreshold, null, null, stopAutomaticallyIfNoNewRequestFetched)
        {
        }

        public ProducerConsumerFramework(PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold, string friendlyNameOfThisInstance, bool stopAutomaticallyIfNoNewRequestFetched)
            : this(null, requestFetcher, requestExecutor, concurrencyThreshold, null, friendlyNameOfThisInstance, stopAutomaticallyIfNoNewRequestFetched)
        {
        }

        public ProducerConsumerFramework(Func<bool> preConditionTaskBeforeInitialization, PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold)
            : this(preConditionTaskBeforeInitialization, requestFetcher, requestExecutor, concurrencyThreshold, null, null, false)
        {
        }

        public ProducerConsumerFramework(Func<bool> preConditionTaskBeforeInitialization, PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold, Action serviceStopper, string friendlyNameOfThisInstance, bool stopAutomaticallyIfNoNewRequestFetched)
        {
            _totalCountOfRequestsExecuted = 0;
            _totalCountOfFailedExecution = 0;
            _preConditionTaskBeforeInitialization = preConditionTaskBeforeInitialization;
            _requestFetcher = requestFetcher;
            _requestExecutor = requestExecutor;
            _concurrencyThreshold = concurrencyThreshold;
            _requests = new BlockingCollection<T>(_concurrencyThreshold);
            _initializationTask = null;
            _cancellationTokenForStartUpJob = null;
            _lockForOpMetrics = new object();

            _stopwatchForOperationMetricsLogging = new Stopwatch();
            _stopwatchForOperationMetricsLogging.Start();

            _isPreConditionTaskExecSuccessful = false;
            _serviceStopper = serviceStopper;

            _isOperationMetricsLoggingEnabled = true;
            _stopAutomaticallyIfNoNewRequestFetched = stopAutomaticallyIfNoNewRequestFetched;
            if (string.IsNullOrEmpty(friendlyNameOfThisInstance))
            {
                _friendlyNameOfThisInstance = string.Format("Hash:{0}", this.GetHashCode());
            }
            else
            {
                _friendlyNameOfThisInstance = string.Format("{0}, Hash:{1}", friendlyNameOfThisInstance, this.GetHashCode());
            }

            Trace.TraceInformation("Instantiated 'Producer-Consumer-Framework' [{0}], Concurrency:{1}", _friendlyNameOfThisInstance, _concurrencyThreshold);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Dumps Internal State Info to Application Log
        /// </summary>
        public void LogInternalState()
        {
            Trace.TraceInformation("============================= PCF =============================");
            Trace.TraceInformation("Dumping state of all threads owned by PCF ({0})", _friendlyNameOfThisInstance);
            Trace.TraceInformation("# of Requests in Queue:{0}, Active Thread Count:{1}", _requests.Count, this.ActiveTaskCount);
            Trace.TraceInformation("ID\tIsCompleted\tIsCanceled\tIsFaulted\tStatus\tException");
            foreach (var id in _allTasks.Keys)
            {
                var t = _allTasks[id];
                Trace.TraceInformation(string.Join("\t", t.Id, t.IsCompleted, t.IsCanceled, t.IsFaulted, t.Status, t.Exception.Message));
            }
            Trace.TraceInformation("============================= --- =============================");
        }

        #region IOpMetrics
        /// <summary>
        /// The date-time when service was started
        /// </summary>
        public DateTime StartTimeUTC
        {
            get
            {
                return _startTimeUTC;
            }
        }

        public int RequestCount
        {
            get
            {
                lock (_lockForOpMetrics)
                {
                    return _totalCountOfRequestsExecuted;
                }
            }
        }

        public int FailureCount
        {
            get
            {
                lock (_lockForOpMetrics)
                {
                    return _totalCountOfFailedExecution;
                }
            }
        }

        /// <summary>
        /// Operation Metrics - Success Ratio in percentage
        /// </summary>
        public int SuccessCount
        {
            get
            {
                int totalCount = this.RequestCount;
                int failCount = this.FailureCount;
                return (totalCount - failCount);
            }
        }

        public float SuccessRatioInPercent
        {
            get
            {
                float totalCount = this.RequestCount;
                float failCount = this.FailureCount;

                if (totalCount <= 0.0)
                    return 0.0f;

                return (100.0f * (totalCount - failCount)) / totalCount;
            }
        }
        #endregion

        public bool EnableOperationMetricsLogging
        {
            get
            {
                return _isOperationMetricsLoggingEnabled;
            }
            set
            {
                _isOperationMetricsLoggingEnabled = value;
            }
        }

        public void WaitForCompletion()
        {
            if (_cancellationTokenForStartUpJob != null)
                wait(_cancellationTokenForStartUpJob);

            if (_cancellationTokenForManager != null)
                wait(_cancellationTokenForManager);

            int count = 0;
            while (true)
            {
                if (this.IsIdle)
                {
                    return;
                }
                Trace.TraceInformation("PCF Waiting for extended period after completion! [Reference:{0}]", _friendlyNameOfThisInstance);
                Thread.Sleep(_timeOutForBlockingCollectionOpInMS);
                count++;
                if (count > 10)
                {
                    Trace.TraceError("FATAL ERROR: PCF WaitForCompletion has become INFINITE, so aborting wait! [Reference:{0}]", _friendlyNameOfThisInstance);
                    return;
                }
            }
        }

        public WaitHandle WaitHandle
        {
            get
            {
                return _waitHandle;
            }
        }

        /// <summary>
        /// Starts Producer-Consumer Process
        /// </summary>
        /// <returns>true if successful; else false, when it's already running</returns>
        public bool Start()
        {
            if (_cancellationTokenForManager != null)
            {
                Trace.TraceError("'Producer-Consumer Framework' [{0}] is already running!", _friendlyNameOfThisInstance);
                return false;
            }
            if (_preConditionTaskBeforeInitialization != null)
            {
                if (_cancellationTokenForStartUpJob != null || _initializationTask != null)
                {
                    Trace.TraceError("Pre-Condition Task for 'Producer-Consumer Framework' [{0}] is already running!", _friendlyNameOfThisInstance);
                    return false;
                }

                _cancellationTokenForStartUpJob = new CancellationTokenSource();
                _initializationTask = Task.Factory.StartNew(() =>
                    {
                        _waitHandle = new AutoResetEvent(false);
                        Trace.TraceInformation("Executing 'Pre-Condition Task' [Retry Frequency:{0}ms] for 'Producer-Consumer-Framework' [{1}, Concurrency:{2}]", _retryIntervalForPreConditionTaskInMS, _friendlyNameOfThisInstance, _concurrencyThreshold);
                        _isPreConditionTaskExecSuccessful = executePreConditionTaskTillSuccessful(_cancellationTokenForStartUpJob, _preConditionTaskBeforeInitialization);
                        if (_isPreConditionTaskExecSuccessful)
                        {
                            Trace.TraceInformation("Pre-Condition Task for 'Producer-Consumer Framework' [{0}] executed successfully!", _friendlyNameOfThisInstance);
                            _waitHandle.Set();
                            return;
                        }
                        Trace.TraceError("Execution of Pre-Condition Task for 'Producer-Consumer Framework' [{0}] encountered error!", _friendlyNameOfThisInstance);
                        if (_serviceStopper != null)
                        {
                            _serviceStopper();
                        }
                        _waitHandle.Set();
                    }
                );
                _initializationTask.ContinueWith((x) =>
                    {
                        if (_isPreConditionTaskExecSuccessful && !_cancellationTokenForStartUpJob.IsCancellationRequested)
                        {
                            Trace.TraceInformation("'Producer-Consumer Framework' [{0}] is being set-up post execution of Pre-Condition Task.", _friendlyNameOfThisInstance);
                            initializeProducerAndConsumer();
                            Trace.TraceInformation("'Producer-Consumer Framework' [{0}] is ready to serve requests.", _friendlyNameOfThisInstance);
                        }
                        _cancellationTokenForStartUpJob = null;
                    });
                return true;
            }
            return initializeProducerAndConsumer();
        }

        /// <summary>
        /// Stops Producer-Consumer Process
        /// </summary>
        public void Stop()
        {
            if (_cancellationTokenForStartUpJob != null)
            {
                Trace.TraceInformation("Stopping Pre-Condition Task for'Producer-Consumer-Framework' [{0}]...", _friendlyNameOfThisInstance);
                _cancellationTokenForStartUpJob.Cancel();
                return;
            }

            if (_cancellationTokenForManager != null)
            {
                Trace.TraceInformation("Stopping 'Producer-Consumer-Framework' Execution [{0}], Concurrency:{1}", _friendlyNameOfThisInstance, _concurrencyThreshold);
                _cancellationTokenForManager.Cancel();
                SafeDispose<CancellationTokenSource>(ref _cancellationTokenForManager);
                Thread.Sleep(_waitTimeBeforeFetchingNextRequest);
                Trace.TraceInformation("'Producer-Consumer Framework' [{0}] has been stopped!", _friendlyNameOfThisInstance);
                if (_onStopped != null)
                {
                    _onStopped(this);
                }
                return;
            }
            Trace.TraceWarning("'Producer-Consumer Framework' [{0}] is not in running state, so can't be terminated!", _friendlyNameOfThisInstance);
        }

        /// <summary>
        /// Checks if this Instance is running
        /// </summary>
        public bool IsStopped
        {
            get
            {
                return (_cancellationTokenForManager == null);
            }
        }

        /// <summary>
        /// Automatically Stops when there is no pending request & no further requests returned by client code. 
        /// </summary>
        public bool AutoStopWhenNoNewRequest
        {
            get
            {
                return _stopAutomaticallyIfNoNewRequestFetched;
            }
        }

        /// <summary>
        /// Checks if Operation Metrics Logging Enabled. 
        /// </summary>
        public bool IsOperationMetricsLoggingEnabled
        {
            get
            {
                return _isOperationMetricsLoggingEnabled;
            }
        }

        /// <summary>
        /// Max # of Tasks to execute concurrently.
        /// </summary>
        public int ConcurrencyThreshold
        {
            get
            {
                return _concurrencyThreshold;
            }
        }

        /// <summary>
        /// Count of Active Tasks.
        /// </summary>
        public int ActiveTaskCount
        {
            get
            {
                if (_allTasks.Count > 0)
                {
                    return (_allTasks.Count - 1);
                }
                return 0;
            }
        }

        /// <summary>
        /// Count of Available Slots for Concurrent Execution of Tasks.
        /// </summary>
        public int AvailableSlots
        {
            get
            {
                int c = _concurrencyThreshold - this.ActiveTaskCount;
                if (c >= 0)
                {
                    return c;
                }
                return 0;
            }
        }

        /// <summary>
        /// Checks if there is any request pending for processing.
        /// </summary>
        public bool IsIdle
        {
            get
            {
                if (_requests.Count <= 1 && ActiveTaskCount == 0)
                {
                    return true;
                }
                return false;
            }
        }

        public event ProducerConsumerFrameworkStopped<T> OnStopped
        {
            add
            {
                _onStopped += value;
            }
            remove
            {
                _onStopped -= value;
            }
        }
        #endregion

        #region Private Methods
        public bool initializeProducerAndConsumer()
        {
            if (_cancellationTokenForManager != null)
            {
                Trace.TraceError("'Producer-Consumer Framework' [{0}] is already running!", _friendlyNameOfThisInstance);
                return false;
            }
            Task t = null;

            _cancellationTokenForManager = new CancellationTokenSource();
            _allTasks = new ConcurrentDictionary<int, Task>(_concurrencyThreshold, _concurrencyThreshold + 1);
            // Start the ONE & ONLY producer
            t = Task.Factory.StartNew(() => fetchRequests(_requests, _cancellationTokenForManager.Token));
            t.ContinueWith(cleanUpUponThreadExecutionCompletion);
            if (!_allTasks.TryAdd(t.Id, t))
            {
                Trace.TraceError("'Producer-Consumer Framework' [{0}]: Failed to initialize manager thread!", _friendlyNameOfThisInstance);
                return false;
            }
            return true;
        }

        private bool launchWorker()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                if (_cancellationTokenForManager != null && _cancellationTokenForManager.Token != null && !_cancellationTokenForManager.Token.IsCancellationRequested)
                {
                    executeNextRequest(_requests, _cancellationTokenForManager.Token);
                }
            });
            t.ContinueWith(cleanUpUponThreadExecutionCompletion);
            if (!_allTasks.TryAdd(t.Id, t))
            {
                Trace.TraceError("'Producer-Consumer Framework' [{0}]: Failed to initialize worker thread!", _friendlyNameOfThisInstance);
                return false;
            }
            return true;
        }

        private void cleanUpUponThreadExecutionCompletion(Task task)
        {
            string reason = (task.IsCompleted) ? "Task Completed" : "Task Terminated";
            if (!task.IsCompleted)
            {
                Trace.TraceWarning("'Producer-Consumer Framework' [{0}] Execution terminated for thread ID:{1}, Reason:{2}.", _friendlyNameOfThisInstance, task.Id, reason);
            }
            Task t;
            if (!_allTasks.TryRemove(task.Id, out t))
            {
                Trace.TraceWarning("'Producer-Consumer Framework' [{0}]: Failed to deregister worker thread having ID:{1}!", _friendlyNameOfThisInstance, task.Id);
                return;
            }
        }

        private void updateOperationMetrics(bool isLastRequestExecSuccessfully)
        {
            lock (_lockForOpMetrics)
            {
                if (_totalCountOfRequestsExecuted == int.MaxValue)
                {
                    Trace.TraceWarning("Resetting counter(s) for operation metrics [Current Success Ratio in Percent:{0.00}]", SuccessRatioInPercent);
                    _totalCountOfRequestsExecuted = 0;
                    _totalCountOfFailedExecution = 0;
                }
                _totalCountOfRequestsExecuted++;
                if (!isLastRequestExecSuccessfully)
                {
                    _totalCountOfFailedExecution++;
                }
            }
        }

        private bool safeExecuteRequest(T request)
        {
            try
            {
                Trace.TraceInformation("Executing request:[{0}]...", request);
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                //
                bool result = this._requestExecutor(request);

                updateOperationMetrics(result);//Update Operation Metrics

                stopWatch.Stop();
                if (result)
                {
                    Trace.TraceInformation("Executed request:[{0}] successfully; Time Taken:{1}ms.", request, stopWatch.Elapsed.TotalMilliseconds);
                    return true;
                }
                Trace.TraceError("Execution Failed for request:[{0}]!", request);
            }
            catch (Exception e)
            {
                Trace.TraceError("Execution of request:[{0}] failed! Exception Caught:{1}", request, e.Message);
                Trace.TraceError(e.StackTrace);
                updateOperationMetrics(false);//Update Operation Metrics
            }
            return false;
        }

        private void executeNextRequest(BlockingCollection<T> bc, CancellationToken ct)
        {
            if (bc.IsCompleted || bc.Count <= 0)
            {
                Trace.TraceWarning("No Request found for execution!");
                return;
            }
            try
            {
                T nextRequest;
                if (!bc.TryTake(out nextRequest, _timeOutForBlockingCollectionOpInMS, ct))
                {
                    Trace.TraceWarning("Failed to dequeue next request, Current Request Count:{0}!", bc.Count);
                    return;
                }
                //
                safeExecuteRequest(nextRequest);
            }
            catch (OperationCanceledException)
            {
                Trace.TraceInformation("'Request Fetching Process' has been terminated, terminating 'Request Execution Process', Current Request Count:{0}!", bc.Count);
            }
        }

        private bool enqueueRequests(BlockingCollection<T> bc, T[] requests, CancellationToken ct)
        {
            if (requests == null || requests.Length <= 0)
            {
                return false;
            }

            try
            {
                foreach (var request in requests)
                {
                    if (request == null)
                    {
                        Trace.TraceError("PCF [{0}]: Encountered 'null' request! Check PCF client code and fix!", _friendlyNameOfThisInstance);
                        continue;
                    }
                    if (ct.IsCancellationRequested)
                    {
                        return false;
                    }

                    bool success = bc.TryAdd(request, _timeOutForBlockingCollectionOpInMS, ct);
                    if (success)
                    {
                        if (this.ActiveTaskCount < _concurrencyThreshold)
                        {
                            Debug.WriteLine("Launching new worker thread, current stats: [ActiveTaskCount:{0}; Concurrency Threshold:{1}]...", this.ActiveTaskCount, _concurrencyThreshold);
                            launchWorker();
                        }
                    }
                    else
                    {
                        Trace.TraceError("PCF [{0}]: Failed to Queue new request:[{1}] [Current Request Count:{2}; Bounded Capacity:{3}]!", _friendlyNameOfThisInstance, request, bc.Count, bc.BoundedCapacity);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                bc.CompleteAdding();
                Trace.TraceInformation("'Request Fetching Process' has been terminated, terminating 'Request Execution Process', Current Request Count:{0}!", bc.Count);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }

        private void fetchRequests(BlockingCollection<T> bc, CancellationToken ct)
        {
            bool continuePcfExecution = true;
            if (_waitHandle != null)
            {
                _waitHandle.Reset();
            }

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    _waitHandle.Set();
                    return;
                }
                int countOfAvailableSlots = bc.BoundedCapacity - (this.ActiveTaskCount + bc.Count);
                //Trace.TraceInformation("CountOfActiveCosnumers:{0}, bc.Count:{1}, countOfAvailableSlots:{2}", this.CountOfActiveCosnumers, bc.Count, countOfAvailableSlots);

                bool isRequestPresent = (countOfAvailableSlots > 0);
                T[] newRequests = null;
                if (isRequestPresent)
                {
                    try
                    {
                        if (continuePcfExecution)
                        {
                            newRequests = _requestFetcher(countOfAvailableSlots, this, ref continuePcfExecution);
                        }
                        else
                        {
                            newRequests = null;
                        }
                        isRequestPresent = (newRequests != null && newRequests.Length > 0);

                        if ((!continuePcfExecution || (!isRequestPresent && _stopAutomaticallyIfNoNewRequestFetched)) && this.IsIdle)
                        {//STOP PCF Execution
                            if (!continuePcfExecution)
                            {
                                Trace.TraceInformation("PCF [{0}]: Stopping Task Scheduling & processing upon client's request.", _friendlyNameOfThisInstance);
                            }
                            else
                            {
                                Trace.TraceInformation("Stopping PCF [{0}] automatically as no new request fetched!", _friendlyNameOfThisInstance);
                            }
                            logOpMetrics();
                            this.Stop();
                            break;
                        }

                        if (isRequestPresent)
                        {
                            if (newRequests.Length > countOfAvailableSlots)
                            {
                                Trace.TraceError("PCF [{0}]: # of Available Slots: {1}; Requests received for processing: {2}!", _friendlyNameOfThisInstance, countOfAvailableSlots, newRequests.Length);
                            }
                            else
                            {
                                Trace.TraceInformation("PCF [{0}]: Fetched {1}# of requests against demand of {2}# of requests.", _friendlyNameOfThisInstance, newRequests.Length, countOfAvailableSlots);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("PCF [{0}]: Failed to fetch next {1}# of Requests! Exception Caught:{2}", _friendlyNameOfThisInstance, countOfAvailableSlots, e.Message);
                        Trace.TraceError(e.StackTrace);
                    }
                }
                if (_isOperationMetricsLoggingEnabled && _stopwatchForOperationMetricsLogging.Elapsed.TotalSeconds > _frequencyOfShowingOpMetricsInSeconds)
                {
                    logOpMetrics();
                    _stopwatchForOperationMetricsLogging.Restart();
                }
                if (!isRequestPresent)
                {
                    Thread.Sleep(_waitTimeBeforeFetchingNextRequest);
                }
                //
                if (isRequestPresent && !enqueueRequests(bc, newRequests, ct))
                {
                    break;
                }
            }
            if (!_isDisposed)
            {
                bc.CompleteAdding();
                if (_waitHandle != null)
                {
                    _waitHandle.Set();
                }
            }
        }

        private void logOpMetrics()
        {
            float f = this.SuccessRatioInPercent;
            string success = (f <= 0.0) ? "0" : f.ToString("#.##");
            Trace.TraceInformation("Operation Metrics for PCF [{0}]: # of Executed Requests:{1}; Success Rate: {2}%;", _friendlyNameOfThisInstance, this.RequestCount, success);
        }

        private static void wait(CancellationTokenSource cts)
        {
            if (cts == null)
            {
                return;
            }

            try
            {
                WaitHandle.WaitAll(new WaitHandle[] { cts.Token.WaitHandle });
            }
            catch (Exception e)
            {
                Trace.TraceInformation("EXPECTED Behavior: Exception:\"{0}\" has been caught & ignored!", e.Message);
                //EXPECTED Behavior - WaitAll() will throw this exception under normal circumstances
            }
        }

        private bool executePreConditionTaskTillSuccessful(CancellationTokenSource cts, Func<bool> task)
        {
            int count = 0;
            while (true)
            {
                if (cts.IsCancellationRequested)
                {
                    return false;
                }
                if (count > 0)
                {
                    Trace.TraceWarning("PCF [{0}]: Executing 'Pre-Condition' task [Retry Count: {1}].", _friendlyNameOfThisInstance, count++);
                }
                else
                {
                    Trace.TraceInformation("PCF [{0}]: Executing 'Pre-Condition' task...", _friendlyNameOfThisInstance);
                }

                try
                {
                    if (task())
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError("PCF [{0}]: Error occured in executing Pre-Condition-Task! Exception:{1}", _friendlyNameOfThisInstance, e.Message);
                    Trace.TraceError(e.StackTrace);
                    return false;
                }

                Trace.TraceWarning("****************************************");
                Trace.TraceWarning("PCF [{0}]: Retrying 'Pre-Condition' task [Retry Count: {1}], Waiting for {2}ms before next retry.", _friendlyNameOfThisInstance, count, _retryIntervalForPreConditionTaskInMS);
                Trace.TraceWarning("****************************************");
                Thread.Sleep(_retryIntervalForPreConditionTaskInMS);
            }
        }

        protected override void doCleanup()
        {
            //cyclic call, resulting in null exception
            //Stop();
            //if (_allTasks != null)
            //{
            //    _allTasks.Clear();
            //}
            SafeDispose<BlockingCollection<T>>(ref this._requests);
            SafeDispose<Task>(ref this._initializationTask);
        }
        #endregion
    }
}

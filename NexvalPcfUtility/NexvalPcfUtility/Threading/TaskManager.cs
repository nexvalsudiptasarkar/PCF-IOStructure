using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Nexval.Framework.PCF.Threading
{
    internal sealed class TaskManager<T> : Disposable, ITaskManager<T>
    {
        #region Data Types
        private sealed class QueuedRequest
        {
            public QueuedRequest(T request, long requestTrackingId)
            {
                Request = request;
                RetryCount = 0;
                RequestTrackingId = requestTrackingId;
            }

            public T Request { get; internal set; }

            public long RequestTrackingId { get; internal set; }

            public int RetryCount { get; set; }

            public override string ToString()
            {
                return string.Format("Id:{0}; Retry Count:{1}; Data:{2}", RequestTrackingId, RetryCount, Request);
            }
        }
        #endregion

        #region Private Members
        private readonly string _friendlyNameOfThisInstance;
        private readonly Func<long, T, bool> _requestExecutor;
        private readonly int _maxRetryCount = 3;
        private long _nextRequestTrackingId = 0;
        private readonly int _concurrencyThreshold;
        private readonly bool _autoStopOnEmptyQueue;
        private object _locker;
        private readonly ConcurrentQueue<QueuedRequest> _requestQueue;
        private readonly ConcurrentQueue<QueuedRequest> _failedRequestQueue;
        private readonly ConcurrentQueue<QueuedRequest> _permanentlyFailedRequestQueue;
        private IProducerConsumerFramework<QueuedRequest> _pcf = null;

        private TaskExecSuccessful<T> _notifyTaskExecSuccessful;
        private TaskExecFailed<T> _notifyTaskExecFailed;
        private TaskManagerStopped<T> _notifyTaskManagerStopped;
        #endregion

        #region Constructor
        public TaskManager(Func<long, T, bool> requestExecutor, string friendlyNameOfThisInstance, int maxCountOfThreadsToCreate, int maxRetryCount)
            : this(requestExecutor, false, friendlyNameOfThisInstance, maxCountOfThreadsToCreate, maxRetryCount)
        {
        }

        public TaskManager(Func<long, T, bool> requestExecutor, bool autoStopOnEmptyQueue, string friendlyNameOfThisInstance, int maxCountOfThreadsToCreate, int maxRetryCount)
        {
            _requestExecutor = requestExecutor;
            _autoStopOnEmptyQueue = autoStopOnEmptyQueue;
            _concurrencyThreshold = maxCountOfThreadsToCreate;
            _maxRetryCount = maxRetryCount;

            _locker = new object();
            _nextRequestTrackingId = 0;
            _requestQueue = new ConcurrentQueue<QueuedRequest>();
            _failedRequestQueue = new ConcurrentQueue<QueuedRequest>();
            _permanentlyFailedRequestQueue = new ConcurrentQueue<QueuedRequest>();

            if (string.IsNullOrEmpty(friendlyNameOfThisInstance))
            {
                _friendlyNameOfThisInstance = string.Format("[Hash:{0}]", this.GetHashCode());
            }
            else
            {
                _friendlyNameOfThisInstance = string.Format("[{0}, Hash:{1}]", friendlyNameOfThisInstance, this.GetHashCode());
            }
            Trace.TraceInformation("TaskManager {0} has been created!", _friendlyNameOfThisInstance);
        }
        #endregion

        #region ITaskManager<T>
        public bool Start()
        {
            if (_pcf == null)
            {
                _pcf = new ProducerConsumerFramework<QueuedRequest>(getRequests, executeQueuedRequest, _concurrencyThreshold, _friendlyNameOfThisInstance, _autoStopOnEmptyQueue);
                _pcf.OnStopped += onPcfStopped;
                _pcf.Start();
                Trace.TraceInformation("TaskManager {0} has been started.", _friendlyNameOfThisInstance);
                return true;
            }
            Trace.TraceError("TaskManager {0} has already been started!", _friendlyNameOfThisInstance);
            return false;
        }

        public bool Stop()
        {
            if (_pcf != null)
            {
                _pcf.Stop();
                Disposable.SafeDispose<IProducerConsumerFramework<QueuedRequest>>(ref _pcf);
                Trace.TraceInformation("TaskManager {0} has been stopped.", _friendlyNameOfThisInstance);
                return true;
            }
            Trace.TraceError("TaskManager {0} was never started!", _friendlyNameOfThisInstance);
            return false;
        }

        /// <summary>
        /// Checks if this Instance is running
        /// </summary>
        public bool IsStopped
        {
            get
            {
                if (_pcf != null)
                {
                    return _pcf.IsStopped;
                }
                return false;
            }
        }

        public long[] EnqueueMultiple(params T[] requests)
        {
            List<long> generatedIds = new List<long>(requests.Length);
            foreach (var request in requests)
            {
                QueuedRequest ur = new QueuedRequest(request, this.NextRequestTrackingId);
                _requestQueue.Enqueue(ur);
                generatedIds.Add(ur.RequestTrackingId);
            }
            return generatedIds.ToArray();
        }

        public long Enqueue(T request)
        {
            QueuedRequest ur = new QueuedRequest(request, this.NextRequestTrackingId);
            _requestQueue.Enqueue(ur);
            return ur.RequestTrackingId;
        }

        public event TaskManagerStopped<T> OnStopped
        {
            add
            {
                _notifyTaskManagerStopped += value;
            }
            remove
            {
                _notifyTaskManagerStopped -= value;
            }
        }

        public IOpMetrics OperationMetrics
        {
            get
            {
                if (_pcf != null & !_pcf.IsStopped)
                {
                    return _pcf;
                }
                return null;
            }
        }

        public event TaskExecSuccessful<T> OnTaskExecSuccessful
        {
            add
            {
                _notifyTaskExecSuccessful += value;
            }
            remove
            {
                _notifyTaskExecSuccessful -= value;
            }
        }

        public event TaskExecFailed<T> OnTaskExecFailed
        {
            add
            {
                _notifyTaskExecFailed += value;
            }
            remove
            {
                _notifyTaskExecFailed -= value;
            }
        }

        public WaitHandle WaitHandle
        {
            get
            {
                if (_pcf != null & !_pcf.IsStopped)
                {
                    return _pcf.WaitHandle;
                }
                return null;
            }
        }

        #endregion

        #region Private Methods

        private long NextRequestTrackingId
        {
            get
            {
                lock (_locker)
                {
                    if (_nextRequestTrackingId + 1 >= long.MaxValue)
                    {
                        Trace.TraceInformation("TaskManager {0}: Resetting next Request Tracking Id to Zero.", _friendlyNameOfThisInstance);
                        _nextRequestTrackingId = 0;
                    }
                    _nextRequestTrackingId++;
                    return _nextRequestTrackingId;
                }
            }
        }

        private void onPcfStopped(IProducerConsumerFramework<TaskManager<T>.QueuedRequest> source)
        {
            if (_notifyTaskManagerStopped != null)
            {
                _notifyTaskManagerStopped(this);
            }
        }

        private bool executeQueuedRequest(QueuedRequest cm)
        {
            bool result = _requestExecutor(cm.RequestTrackingId, cm.Request);
            if (!result)
            {
                cm.RetryCount = cm.RetryCount + 1;
                if (cm.RetryCount < _maxRetryCount)
                {
                    //Keep this for retrying later
                    _failedRequestQueue.Enqueue(cm);
                }
                else
                {
                    Trace.TraceError("TaskManager {0}: FATAL ERROR: Permanent failure encountered for executing Request:{1}, Tracking Id:{2} after retrying {3}# of times!", _friendlyNameOfThisInstance, cm.Request, cm.RequestTrackingId, cm.RetryCount);
                    //Stop processing further requests if it's set to do so.
                    if (_autoStopOnEmptyQueue)
                    {
                        //Stop processing further requests - return no further requests when there is any permanent failure
                        _permanentlyFailedRequestQueue.Enqueue(cm);
                    }
                }
                if (_notifyTaskExecFailed != null)
                {
                    bool shouldTaskManagerBeTerminated = false;
                    bool isPermanentFailure = cm.RetryCount >= _maxRetryCount;
                    _notifyTaskExecFailed(this, cm.RequestTrackingId, cm.Request, cm.RetryCount, isPermanentFailure, ref shouldTaskManagerBeTerminated);
                }
                return false;
            }
            if (_notifyTaskExecSuccessful != null)
            {
                _notifyTaskExecSuccessful(this, cm.RequestTrackingId, cm.Request);
            }
            return result;
        }

        private QueuedRequest[] getRequests(int expectedCountOfRequests, IProducerConsumerFramework<QueuedRequest> source, ref bool continuePcfExecution)
        {
            continuePcfExecution = true;//PCF will never stop
            if (_autoStopOnEmptyQueue && _permanentlyFailedRequestQueue.Count > 0)
            {//Stop processing further requests - return no further requests when there is any permanent failure
                Trace.TraceInformation("TaskManager {0}: Permanent Failure encountered for {1}# of failed requests. No further requests will be executed!", _friendlyNameOfThisInstance, _permanentlyFailedRequestQueue.Count);
                continuePcfExecution = false;//Stop PCF.
                return null;
            }

            Debug.WriteLine(string.Format("TaskManager {0}: Fetching requests from general request queue...", _friendlyNameOfThisInstance));
            QueuedRequest[] collection = getNextSetOfRequests(expectedCountOfRequests, _requestQueue);
            if (collection == null || collection.Length <= 0)
            {
                Debug.WriteLine(string.Format("TaskManager {0}: Fetching requests from Failed request queue...", _friendlyNameOfThisInstance));
                collection = getNextSetOfRequests(expectedCountOfRequests, _failedRequestQueue);
                if (collection != null && collection.Length > 0)
                {
                    Trace.TraceInformation("TaskManager {0}: Fetched {1}# of requests from Failed request queue.", _friendlyNameOfThisInstance, collection.Length);
                }
            }
            if (collection != null && collection.Length > 0)
            {
                return collection;
            }
            Debug.WriteLine(string.Format("TaskManager {0}: No requests found.", _friendlyNameOfThisInstance));
            return null;
        }

        private QueuedRequest[] getNextSetOfRequests(int count, ConcurrentQueue<QueuedRequest> queue)
        {
            if (queue.Count == 0)
                return null;

            List<QueuedRequest> collection = new List<QueuedRequest>(count);
            while (true)
            {
                QueuedRequest qr = null;
                if (!queue.TryDequeue(out qr))
                {
                    Trace.TraceWarning("TaskManager {0}: Failed to fetch next request from request queue!", _friendlyNameOfThisInstance);
                    continue;
                }
                collection.Add(qr);
                if (collection.Count >= count || queue.Count == 0)
                {
                    break;
                }
            }
            return collection.Count > 0 ? collection.ToArray() : null;
        }

        protected override void doCleanup()
        {
            if (_pcf != null)
            {
                _pcf.Stop();
                Disposable.SafeDispose<IProducerConsumerFramework<QueuedRequest>>(ref _pcf);
            }
        }
        #endregion
    }
}

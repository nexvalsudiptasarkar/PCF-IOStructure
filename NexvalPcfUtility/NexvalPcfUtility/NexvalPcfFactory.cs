using Nexval.Framework.PCF.Threading;
using System;

namespace Nexval.Framework.PCF
{
    public static class NexvalPcfFactory
    {
        #region Member Variables
        #endregion

        #region Public Methods

        #region Task Manager
        public static ITaskManager<T> GetTaskManager<T>(Func<long, T, bool> requestExecutor, bool stopAutomaticallyIfNoNewRequestFetched, string friendlyNameOfThisInstance, int maxCountOfThreadsToCreate, int maxRetryCount)
        {
            return new TaskManager<T>(requestExecutor, stopAutomaticallyIfNoNewRequestFetched, friendlyNameOfThisInstance, maxCountOfThreadsToCreate, maxRetryCount);
        }

        public static ITaskManager<T> GetTaskManager<T>(Func<long, T, bool> requestExecutor, string friendlyNameOfThisInstance, int maxCountOfThreadsToCreate, int maxRetryCount)
        {
            return new TaskManager<T>(requestExecutor, friendlyNameOfThisInstance, maxCountOfThreadsToCreate, maxRetryCount);
        }
        #endregion

        #region Producer Consumer Framework

        public static IProducerConsumerFramework<T> GetPcf<T>(PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold)
        {
            return new ProducerConsumerFramework<T>(requestFetcher, requestExecutor, concurrencyThreshold);
        }

        public static IProducerConsumerFramework<T> GetPcf<T>(PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold, bool stopAutomaticallyIfNoNewRequestFetched)
        {
            return new ProducerConsumerFramework<T>(requestFetcher, requestExecutor, concurrencyThreshold, stopAutomaticallyIfNoNewRequestFetched);
        }

        public static IProducerConsumerFramework<T> GetPcf<T>(PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold, string friendlyNameOfThisInstance, bool stopAutomaticallyIfNoNewRequestFetched)
        {
            return new ProducerConsumerFramework<T>(requestFetcher, requestExecutor, concurrencyThreshold, friendlyNameOfThisInstance, stopAutomaticallyIfNoNewRequestFetched);
        }

        public static IProducerConsumerFramework<T> GetPcf<T>(Func<bool> preConditionTaskBeforeInitialization, PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold)
        {
            return new ProducerConsumerFramework<T>(preConditionTaskBeforeInitialization, requestFetcher, requestExecutor, concurrencyThreshold);
        }

        public static IProducerConsumerFramework<T> GetPcf<T>(Func<bool> preConditionTaskBeforeInitialization, PcfRequestFetcher<T> requestFetcher, Func<T, bool> requestExecutor, int concurrencyThreshold, Action serviceStopper, string friendlyNameOfThisInstance, bool stopAutomaticallyIfNoNewRequestFetched)
        {
            return new ProducerConsumerFramework<T>(preConditionTaskBeforeInitialization, requestFetcher, requestExecutor, concurrencyThreshold, serviceStopper, friendlyNameOfThisInstance, stopAutomaticallyIfNoNewRequestFetched);
        }
        #endregion

        #endregion
    }
}

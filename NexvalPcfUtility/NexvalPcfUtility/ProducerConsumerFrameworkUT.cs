using Nexval.Framework.PCF.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Nexval.Framework.PCF
{
#if DEBUG
    public sealed class ProducerConsumerFrameworkUT
    {
        #region Member Variables
        private static Func<int, int> Factorial = x => x < 0 ? -1 : x == 1 || x == 0 ? 1 : x * Factorial(x - 1);
        #endregion

        #region Constructor
        private ProducerConsumerFrameworkUT()
        {
        }
        #endregion

        #region Public Methods
        public static void Test()
        {
            ProducerConsumerFrameworkUT ut = new ProducerConsumerFrameworkUT();
      
            IProducerConsumerFramework<int> pcf = new ProducerConsumerFramework<int>(ut.generateRequests, ut.executeRequests, 10);
            pcf.Start();
            pcf.WaitForCompletion();
        }
        #endregion

        #region Private Methods
        private int[] generateRequests(int countOfRequestsToGenerate, IProducerConsumerFramework<int> source, ref bool continuePcfExecution)
        {
            List<int> requests = new List<int>();
            Random r = new Random();
            int count = countOfRequestsToGenerate;
            count = r.Next(1, countOfRequestsToGenerate);//Randomize request count - DON'T full-fill exact demand
            for (int i = 0; i < count; i++)
            {
                requests.Add(r.Next(10, 15));
            }
            return (requests.Count > 0) ? requests.ToArray() : null;
        }

        private bool executeRequests(int number)
        {
            try
            {
                int result = Factorial(number);
                Thread.Sleep(number * 1000);
                Trace.TraceInformation("{0}!={1}", number, result);
                return true;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
            }
            return false;
        }
        #endregion
    }
#endif
}

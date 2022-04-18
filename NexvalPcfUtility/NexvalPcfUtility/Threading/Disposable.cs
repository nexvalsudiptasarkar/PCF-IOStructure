using System;

namespace Nexval.Framework.PCF.Threading
{
    /// <summary>
    /// Utility to Dispose classes correctly
    /// </summary>
    /// <remarks>The derived classes MUST override the method doCleanup() to perform clean-up activities</remarks>
    internal abstract class Disposable : IDisposable
    {
        #region Member variales
        protected bool _isDisposed;
        #endregion

        #region Constructors
        protected Disposable()
        {
            this._isDisposed = false;
        }
        #endregion

        /// <summary>
        /// Calls Dispose method on a given object if not null
        /// </summary>
        /// <typeparam name="T">Any IDisposable object</typeparam>
        /// <param name="objectToDispose">The object to dispose</param>
        public static void SafeDispose<T>(ref T objectToDispose) where T : class, IDisposable
        {
            if (objectToDispose != null)
            {
                objectToDispose.Dispose();
                objectToDispose = null;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._isDisposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    doCleanup();
                }
            }
            this._isDisposed = true;
        }
        /// <summary>
        /// Disposes all managed and unmanaged resources
        /// </summary>
        protected abstract void doCleanup();
        #endregion
    }
}

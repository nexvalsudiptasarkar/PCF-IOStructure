using System;

namespace Nexval.Framework.PCF.Threading
{
    /// <summary>
    /// Helper class for Executing a Task at the time of creation & another task at the time of disposing
    /// </summary>
    internal sealed class AutoExecuteStartFinishTasks : Disposable
    {
        #region Data Types
        public enum ActionExecType { None, OnlyStart, OnlyEnd, All };
        #endregion

        #region Member variales
        private readonly Action _taskAtEnd;
        private ActionExecType _executeAllActions;
        #endregion

        #region Constructors
        public AutoExecuteStartFinishTasks(Action taskAtStart, Action taskAtEnd)
            : this(ActionExecType.All, taskAtStart, taskAtEnd)
        {
        }

        public AutoExecuteStartFinishTasks(bool executeAllActions, Action taskAtStart, Action taskAtEnd)
            : this((executeAllActions) ? ActionExecType.All : ActionExecType.None, taskAtStart, taskAtEnd)
        {
        }

        public AutoExecuteStartFinishTasks(ActionExecType execType, Action taskAtStart, Action taskAtEnd)
        {
            this._executeAllActions = execType;
            this._taskAtEnd = taskAtEnd;
            if (this._executeAllActions == ActionExecType.All || this._executeAllActions == ActionExecType.OnlyStart)
            {
                taskAtStart();
            }
        }
        #endregion

        #region Properties
        public bool ExecuteActionAtEnd
        {
            get
            {
                return (this._executeAllActions == ActionExecType.All || this._executeAllActions == ActionExecType.OnlyEnd);
            }
            set
            {
                this._executeAllActions = (value) ? ActionExecType.OnlyEnd : (this._executeAllActions == ActionExecType.OnlyStart) ? ActionExecType.OnlyStart : ActionExecType.None;
            }
        }
        #endregion

        #region Overrides
        protected override void doCleanup()
        {
            if (this._executeAllActions == ActionExecType.All || this._executeAllActions == ActionExecType.OnlyEnd)
            {
                this._taskAtEnd();
            }
        }
        #endregion
    }
}
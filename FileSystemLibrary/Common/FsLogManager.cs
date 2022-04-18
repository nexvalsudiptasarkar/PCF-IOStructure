using System;
using System.Collections;
using System.Diagnostics;

namespace FileSystemLib.Common
{
    internal enum Severity
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    internal static class FsLogManager
    {
        static FsLogManager()
        {
        }

        #region Helpers - for Quick Calls
        public static void Info(string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            writeToLog(message, Severity.Info);
        }

        public static void Debug(string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            writeToLog(message, Severity.Debug);
        }

        public static void Warn(Exception e, string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            Fatal(message, Severity.Warn);
            logExceptionDetail(e, (x) => writeToLog(x, Severity.Warn));
        }

        public static void Warn(string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            writeToLog(message, Severity.Warn);
        }

        public static void Fatal(string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            writeToLog(message, Severity.Fatal);
        }

        public static void Fatal(Exception e, string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            Fatal(message, Severity.Fatal);
            logAllExceptions(e, (x) => writeToLog(x, Severity.Fatal));
        }
        #endregion

        #region Helpers to write Exception Detail by implementing 'IObjectRenderer' - if no ready-made class is available in log4Net logger

        private static void logAllExceptions(Exception e, Action<string> logger)
        {
            logger(string.Format("Exception Detail..."));
            while (e != null)
            {
                logExceptionDetail(e, logger);
                e = e.InnerException;
            }
        }

        private static void logExceptionDetail(Exception e, Action<string> logger)
        {
            logger(string.Format("Type: {0}", e.GetType().FullName));
            logger(string.Format("Message: {0}", e.Message));
            logger(string.Format("Source: {0}", e.Source));
            logger(string.Format("TargetSite: {0}", e.TargetSite));
            //Write Exception Data
            logger(string.Format("Exception Data - {0}# of elements", e.Data.Count));
            foreach (DictionaryEntry entry in e.Data)
            {
                logger(string.Format("{0}: {1}", entry.Key, entry.Value));
            }
            logger(string.Format("StackTrace: {0}", e.StackTrace));
        }

        private static void writeToLog(string message, Severity severity)
        {
            switch (severity)
            {
                case Severity.Info:
                    System.Diagnostics.Trace.TraceInformation(message);
                    break;
                case Severity.Warn:
                    System.Diagnostics.Trace.TraceWarning(message);
                    break;
                case Severity.Debug:
                    System.Diagnostics.Debug.WriteLine(message);
                    break;
                case Severity.Error:
                case Severity.Fatal:
                    System.Diagnostics.Trace.TraceError(message);
                    break;
            }
        }
        #endregion
    }
}
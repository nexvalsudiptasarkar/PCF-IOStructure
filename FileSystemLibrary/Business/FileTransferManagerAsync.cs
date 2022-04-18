using Amazon.S3;
using Amazon.S3.Model;
using Nexval.Framework.PCF;
using Nexval.Framework.PCF.Threading;
using FileSystemLib.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FileSystemLib
{
    internal sealed class FileTransferManagerAsync : IFileTransferManagerAsync
    {
        #region Data Types
        #endregion

        #region Private Members
        private const int _maxRetryCount = 3;
        private const int _concurrency = 5;
        private readonly IS3OpsInternal _s3Ops = null;
        private ITaskManager<FileTransferRequest> _taskManager = null;
        private FileTransferSuccessful _notifyFileTransferSuccessful;
        private FileTransferFailed _notifyFileTransferFailed;
        private EventHandler<FileTransferProgressArgs> _fileTransferProgressEvent;
        #endregion

        #region Constructor
        internal FileTransferManagerAsync(IS3OpsInternal s3Ops)
        {
            _taskManager = null;
            _s3Ops = s3Ops;
            _s3Ops.FileTransferProgressEvent += onFileTransferProgressEvent;
            initialize(_concurrency, _maxRetryCount);

            Trace.TraceInformation("FileTransferManagerAsync instantiated with IS3Ops#:{0}, Hashcode:{1}.", _s3Ops.GetHashCode(), GetHashCode());
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Uploads a file to S3 Asynchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <returns>true if successful; else false</returns>
        public long Upload(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string filePathOnDisk, bool trackProgress)
        {
            KeyValuePair<string, string>? s3Detail = locator.GetBucketAndFileLocation(projectId, accountId, fileNameInS3WithoutPath);

            if (s3Detail == null)
            {
                string s = string.Format("Failed to determine File Location for:{0} in AWS-S3! Reference Project Id:{1}, Account Id:{2}", fileNameInS3WithoutPath, projectId, accountId);
                FsLogManager.Fatal(s);
                return -1;
            }
            string bucketName = s3Detail.Value.Key;
            string s3KeyName = s3Detail.Value.Value;

            FileTransferRequest r = new FileTransferRequest(filePathOnDisk, bucketName, s3KeyName, FileTransferRequestType.Upload, trackProgress);
            return _taskManager.Enqueue(r);
        }

        public long Download(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string destinationFileInLocalFs, bool trackProgress)
        {
            KeyValuePair<string, string>? s3Detail = locator.GetBucketAndFileLocation(projectId, accountId, fileNameInS3WithoutPath);

            if (s3Detail == null)
            {
                string s = string.Format("Failed to determine File Location for:{0} in AWS-S3! Reference Project Id:{1}, Account Id:{2}", fileNameInS3WithoutPath, projectId, accountId);
                FsLogManager.Fatal(s);
                return -1;
            }
            string bucketName = s3Detail.Value.Key;
            string s3KeyName = s3Detail.Value.Value;

            FileTransferRequest r = new FileTransferRequest(destinationFileInLocalFs, bucketName, s3KeyName, FileTransferRequestType.Download, trackProgress);
            return _taskManager.Enqueue(r);
        }

        public event FileTransferSuccessful OnFileTransferSuccessful
        {
            add
            {
                _notifyFileTransferSuccessful += value;
            }
            remove
            {
                _notifyFileTransferSuccessful -= value;
            }
        }

        public event FileTransferFailed OnFileTransferFailed
        {
            add
            {
                _notifyFileTransferFailed += value;
            }
            remove
            {
                _notifyFileTransferFailed -= value;
            }
        }

        /// <summary>
        /// Event for receiving File Transfer Progress
        /// Caller must call Upload/Download APIs with a parameter for enabling tracking. 
        /// </summary>
        public event EventHandler<FileTransferProgressArgs> FileTransferProgressEvent
        {
            add
            {
                _fileTransferProgressEvent += value;
            }
            remove
            {
                _fileTransferProgressEvent -= value;
            }
        }
        #endregion

        #region Private Methods

        private void initialize(int concurrency, int maxRetryCount)
        {
            _taskManager = NexvalPcfFactory.GetTaskManager<FileTransferRequest>(executeRequests, false, "File-Transfer-Manager", concurrency, maxRetryCount);
            _taskManager.OnTaskExecFailed += onTaskExecFailed;
            _taskManager.OnTaskExecSuccessful += onTaskExecSuccessful;
            _taskManager.OnStopped += onTaskManagerStopped;
            _taskManager.Start();
        }

        private bool executeRequests(long trackingId, FileTransferRequest request)
        {
            try
            {
                bool b = false;

                request.UpdateTime = DateTime.Now;
                request.Status = RequestStatus.InProgress;
                if (request.TransferType == FileTransferRequestType.Upload)
                {
                    b = _s3Ops.UploadFile(request.DiskFilePath, request.S3BucketName, request.S3FilePath, request.IsMarkedForProgressTracking);
                }
                if (request.TransferType == FileTransferRequestType.Download)
                {
                    b = _s3Ops.DownloadFile(request.S3BucketName, request.S3FilePath, request.DiskFilePath, request.IsMarkedForProgressTracking);
                }

                if (b)
                {
                    request.Status = RequestStatus.Successful;
                    return true;
                }
                request.Status = RequestStatus.Failed;
                return false;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
            }
            return false;
        }

        #region Event Handlers - ITaskManager
        private void onTaskExecFailed(ITaskManager<FileTransferRequest> source, long requestTrackingId, FileTransferRequest request, int retryCount, bool isPermanentlyFailed, ref bool shouldTaskManagerBeTerminated)
        {
            Trace.TraceError("Exec Failed for Task Id:{0}, Data:{1}, Retry Count:{2}!", requestTrackingId, request, retryCount);
            request.UpdateTime = DateTime.Now;
            request.RetryCount = retryCount;
            request.TrackingId = requestTrackingId;
            if (_notifyFileTransferFailed != null)
            {
                _notifyFileTransferFailed(request);
            }
        }

        private void onTaskExecSuccessful(ITaskManager<FileTransferRequest> source, long requestTrackingId, FileTransferRequest request)
        {
            Trace.TraceInformation("Exec Successful for Task Id:{0}, Data:{1}.", requestTrackingId, request);

            request.TrackingId = requestTrackingId;
            request.UpdateTime = DateTime.Now;
            if (_notifyFileTransferSuccessful != null)
            {
                _notifyFileTransferSuccessful(request);
            }
        }

        private void onTaskManagerStopped(ITaskManager<FileTransferRequest> source)
        {
            _taskManager.OnStopped -= onTaskManagerStopped;
            _taskManager.OnTaskExecFailed -= onTaskExecFailed;
            _taskManager.OnTaskExecSuccessful -= onTaskExecSuccessful;
            _taskManager = null;
        }
        #endregion

        private void onFileTransferProgressEvent(object sender, FileTransferProgressArgs e)
        {
            if (_fileTransferProgressEvent != null)
            {
                _fileTransferProgressEvent(this, e);
            }
        }
        #endregion
    }
}
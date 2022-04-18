using System;
using System.IO;

namespace FileSystemLib
{
    public enum FileTransferRequestType { None, Upload, Download };

    public sealed class FileTransferRequest
    {
        internal FileTransferRequest(string filePathDisk, string s3BucketName, string s3FilePath, FileTransferRequestType transferType, bool trackProgress)
        {
            DiskFilePath = filePathDisk;
            TransferType = transferType;
            S3BucketName = s3BucketName;
            S3FilePath = s3FilePath;
            Status = RequestStatus.NotQueued;
            DiskFileSize = -1;
            if (transferType == FileTransferRequestType.Upload)
            {
                FileInfo fi = new FileInfo(DiskFilePath);
                DiskFileSize = fi.Length;
            }
            IsMarkedForProgressTracking = trackProgress;
            UpdateTime = DateTime.Now;
        }

        public bool IsMarkedForProgressTracking { get; internal set; }

        public FileTransferRequestType TransferType { get; internal set; }

        public long TrackingId { get; internal set; }

        public string DiskFilePath { get; internal set; }

        public long DiskFileSize { get; internal set; }

        public string S3BucketName { get; internal set; }

        public string S3FilePath { get; internal set; }

        public RequestStatus Status { get; internal set; }

        public DateTime UpdateTime { get; internal set; }

        public int RetryCount { get; internal set; }

        public override string ToString()
        {
            return string.Format("Id:{0}; Type:{1}; DiskFile:{2}; S3-Bucket:{3}; S3-Key:{4}; Status:{5}", TrackingId, TransferType, DiskFilePath, this.S3BucketName, S3FilePath, Status);
        }
    }

    /// <summary>
    /// Status of Asynchronous Tasks (e.g. Upload, Download)
    /// </summary>
    public enum RequestStatus { NotQueued, InProgress, Successful, Failed };

    /// <summary>
    /// Raised when a Task (Upload or Download) Executed successfully
    /// </summary>
    /// <param name="request">The input for the Task Executed</param>
    public delegate void FileTransferSuccessful(FileTransferRequest request);

    /// <summary>
    /// Raised when a Task (Upload or Download) Execution Failed
    /// </summary>
    /// <param name="request">The input for the Task Executed</param>
    public delegate void FileTransferFailed(FileTransferRequest request);

    public interface IFileTransferManagerAsync
    {
        /// <summary>
        /// Uploads a file to S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <returns>A +VE value to track download status, if successful; else -VE value indicating Failure</returns>
        long Upload(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string filePathOnDisk, bool trackProgress);

        /// <summary>
        /// Downloads a file from S3 asynchronously. The file can be either be made of multiple physical Chunks in S3 or an integrated one in S3.
        /// In the process of download, the following gets done.
        ///     a) Determines S3 locaion & check if exists. 
        ///     b) If not, contact HBase Service for availability (if queued for Stitching). 
        ///     c) If Queued, then download chunks, stitch, serve the file & then upload to S3 in a low priority queue.
        /// </summary>
        /// <param name="projectId">Project Id in which the document exists</param>
        /// <param name="accountId">Account Id in which the project exists</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="fileNameInS3WithoutPath">The Destination File Path In Local File System or Network Share</param>
        /// <returns>A +VE value to track download status, if successful; else -VE value indicating Failure</returns>
        long Download(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string destinationFileInLocalFs, bool trackProgress);

        /// <summary>
        /// Raised on successful transfer of a file.
        /// </summary>
        event FileTransferSuccessful OnFileTransferSuccessful;

        /// <summary>
        /// Raised when transfer of a file is failed.
        /// </summary>
        event FileTransferFailed OnFileTransferFailed;
  
        /// <summary>
        /// Event for receiving File Transfer Progress
        /// Caller must call Upload/Download APIs with a parameter for enabling tracking. 
        /// </summary>
        event EventHandler<FileTransferProgressArgs> FileTransferProgressEvent;
    }
}
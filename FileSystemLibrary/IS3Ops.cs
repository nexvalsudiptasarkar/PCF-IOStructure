using FileSystemLib.Business;
using System;
using System.Collections.Generic;
using System.IO;

namespace FileSystemLib
{
    /// <summary>
    /// Utility to locate a file in S3 for a given project, account & storage Id.
    /// The host application/service should implement this.
    /// </summary>
    public interface IS3FileLocator
    {
        /// <summary>
        /// Gets File Location in S3 along with S3-Bucket-Name. File location varies with Account.
        /// </summary>
        /// <param name="projectId">Project Id for which file location needs to be identified</param>
        /// <param name="accountId">Account Id in which the Project/File resides</param>
        /// <param name="fileNameInS3WithoutPath">The File Name in S3, Excluding Path/Folder/Location info</param>
        /// <returns>
        /// Returns null in case of failure.
        /// Returns the below KeyValuePair, if successful  
        ///     The S3-Bucket-Name as Key.
        ///     S3-Key of the file along with path as Value.
        /// </returns>
        KeyValuePair<string, string>? GetBucketAndFileLocation(int projectId, int accountId, string fileNameInS3WithoutPath);

        /// <summary>
        /// Gets Root Folder Path in S3 along with S3-Bucket-Name. File location varies with Account.
        /// </summary>
        /// <param name="projectId">Project Id for which Root Folder Path needs to be identified</param>
        /// <param name="accountId">Account Id in which the Project/Folder resides</param>
        /// <returns>
        /// Returns null in case of failure.
        /// Returns the below KeyValuePair, if successful  
        ///     The S3-Bucket-Name as Key.
        ///     S3-Key of the Root-Folder along with path as Value.
        /// </returns>
        KeyValuePair<string, string>? GetBucketAndRootFolderPath(int projectId, int accountId);
    }

    public interface IS3OpsInternal : IS3Ops
    {
        /// <summary>
        /// File Size of a given file
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file, should contain file name along with file path</param>
        /// <returns>File Size of a given file if successful; returns -VE value on failure</returns>
        long GetFileSize(string bucketName, string s3Key);

        /// <summary>
        /// Generates Pre-Signed url for Downloading a file
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be downloaded, should contain file name along with file path</param>
        /// <param name="validity">Validity of the Pre-Signed url</param>
        /// <returns>Pre-Signed url if successful; null if failed</returns>
        string GeneratePreSignedDownloadUrl(string bucketName, string s3Key, TimeSpan validity, string downloadFileName = null);

        /// <summary>
        /// Downloads a file from S3 Synchronously
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be deleted, should contain file name along with file path</param>
        /// <param name="targetPathOnDisk">The Destination File Path In Local File System or Network Share</param>
        /// <returns>true if successful; else false</returns>
        bool DownloadFile(string bucketName, string s3Key, string targetPathOnDisk);

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(string filePathOnDisk, string bucketName, string s3Key);

        /// <summary>
        /// Uploads a file from S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be downloaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(string filePathOnDisk, string bucketName, string s3Key, bool trackProgress);

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(byte[] fileContent, string bucketName, string s3Key);

        /// <summary>
        /// Creates a Folder under a Bucket from S3.
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the operation needs to be performed</param>
        /// <param name="s3Key">S3 Key of the Folder to be created, should contain Folder name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool CreateFolder(string bucketName, string s3Key);
    }

    /// <summary>
    /// I/O Operations in AWS-S3.
    /// </summary>
    public interface IS3Ops : IDisposable
    {
        /// <summary>
        /// File Size of a given file
        /// </summary>
        /// <param name="projectId">Project Id in which the document exists</param>
        /// <param name="accountId">Account Id in which the document exists</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <returns>File Size of a given file (bytes) if successful; returns -VE value on failure</returns>
        long GetFileSize(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator);

        /// <summary>
        /// Generates Pre-Signed url for Downloading a file
        /// </summary>
        /// <param name="projectId">Project Id for which to generate Pre-Signed url</param>
        /// <param name="accountId">Account Id for which to generate Pre-Signed url</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="validity">Validity of the Pre-Signed url</param>
        /// <returns>Pre-Signed url if successful; null if failed</returns>
        string GeneratePreSignedDownloadUrl(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, TimeSpan validity, string downloadFileName = null);

        /// <summary>
        /// Uploads a file to S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string filePathOnDisk);

        /// <summary>
        /// Uploads a file from S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string filePathOnDisk, bool trackProgress);

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, byte[] fileContent);

        /// <summary>
        /// Uploads content of a file to S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, byte[] fileContent, bool trackProgress);

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, Stream inputStream);

        /// <summary>
        /// Uploads content of a file to S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, Stream inputStream, bool trackProgress);

        /// <summary>
        /// Get Directory Content i.e. Files & Folders
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the folder to be queried</param>
        /// <returns>Directory Content i.e. Files & Folders if successful; else null</returns>
        string[] GetDirectoryContent(string bucketName, string s3Key);

        /// <summary>
        /// Deletes a file from S3.
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be deleted, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool DeleteFile(string bucketName, string s3Key);

        /// <summary>
        /// Downloads a file from S3 Synchronously. The file can either made of multiple physical Chunks which are not stitched yet or available as an integrated file.
        /// In the process of download, the following gets done.
        ///     a) Determines S3 locaion & check if exists. 
        ///     b) If not, contact HBase Service for availability (if queued for Stitching). 
        ///     c) If Queued, then download chunks, stitch, serve the file & then upload to S3 in a low priority queue.
        /// </summary>
        /// <param name="projectId">Project Id in which the document exists</param>
        /// <param name="accountId">Account Id in which the project exists</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="destinationFileInLocalFs">The Destination File Path In Local File System or Network Share</param>
        /// <returns>true if successful; else false</returns>
        bool DownloadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string destinationFileInLocalFs);

        /// <summary>
        /// Downloads a file from S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be deleted, should contain file name along with file path</param>
        /// <param name="targetPathOnDisk">The Destination File Path In Local File System or Network Share</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool DownloadFile(string bucketName, string s3Key, string targetPathOnDisk, bool trackProgress);

        /// <summary>
        /// Copies file from one locattion to another in a given bucket
        /// </summary>
        /// <param name="bucketName">Bucket Name from which copy is to be performed(source bucket)</param>
        /// <param name="s3KeySource">S3 Key of the Source file to be copied</param>
        /// <param name="s3KeyDestination">S3 Key of the Destination file to be copied</param>
        /// /// <param name="destinationBucket">Bucket Name in which copy to be performed(destinationBucket)</param>
        /// <returns>true if successful; else false</returns>
        bool CopyFile(string bucketName, string s3KeySource, string s3KeyDestination, string destinationBucket = null);

        /// <summary>
        /// Transfers File in Async Manner
        /// </summary>
        IFileTransferManagerAsync FileTransferManager { get; }

        /// <summary>
        /// Event for receiving File Transfer Progress
        /// Caller must call Upload/Download APIs with a parameter for enabling tracking. 
        /// </summary>
        event EventHandler<FileTransferProgressArgs> FileTransferProgressEvent;
    }
}
using System;
using System.IO;

namespace FileSystemLib.Business
{
    public interface IFileUploadUtil
    {
        /// <summary>
        /// Event for receiving File Transfer Progress
        /// Caller must call Upload/Download APIs with a parameter for enabling tracking. 
        /// </summary>
        event EventHandler<FileTransferProgressArgs> FileTransferProgressEvent;

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool Upload(byte[] fileContent, string bucketName, string s3Key);

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool Upload(byte[] fileContent, string bucketName, string s3Key, bool trackProgress);

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool Upload(Stream inputStream, string bucketName, string s3Key);

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool Upload(Stream inputStream, string bucketName, string s3Key, bool trackProgress);

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        bool Upload(string filePathOnDisk, string bucketName, string s3Key);

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        bool Upload(string filePathOnDisk, string bucketName, string s3Key, bool trackProgress);
    }
}

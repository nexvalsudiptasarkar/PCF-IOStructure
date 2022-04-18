using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FileSystemLib.Common;
using System;
using System.IO;

namespace FileSystemLib
{
    internal sealed class FileUploadUtil : FileSystemLib.Business.IFileUploadUtil
    {
        #region Data Types
        #endregion

        #region Private Members
        private readonly ISharedAppSettings _sharedAppSettings;
        private readonly IAmazonS3 _s3Client;

        private EventHandler<FileTransferProgressArgs> _fileTransferProgressEvent;
        #endregion

        #region Constructor
        internal FileUploadUtil(ISharedAppSettings sharedAppSettings, IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
            _sharedAppSettings = sharedAppSettings;
            //High-Level API File Uploading Process: http://docs.aws.amazon.com/AmazonS3/latest/dev/HLuploadFileDotNet.html
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool Upload(Stream inputStream, string bucketName, string s3Key)
        {
            return Upload(inputStream, bucketName, s3Key, false);
        }

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool Upload(Stream inputStream, string bucketName, string s3Key, bool trackProgress)
        {
            try
            {
                TransferUtilityUploadRequest tr = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    InputStream = inputStream,
                    Key = s3Key
                };

                using (TransferUtility ftu = new TransferUtility(_s3Client))
                {
                    FsLogManager.Debug("Uploading Stream to Bucket:{0}, S3Key:{1} [Stream Hash:{2}; Type:{3}]", bucketName, s3Key, inputStream.GetHashCode(), inputStream.GetType());
                    if (trackProgress)
                    {
                        tr.UploadProgressEvent += new EventHandler<UploadProgressArgs>(uploadRequest_UploadPartProgressEvent);
                    }
                    ftu.Upload(inputStream, bucketName, s3Key);
                    if (trackProgress)
                    {
                        tr.UploadProgressEvent -= new EventHandler<UploadProgressArgs>(uploadRequest_UploadPartProgressEvent);
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Upload failed for Input Stream! Destination Bucket:{0}, S3Key:{1} [Stream Hash:{2}; Type:{3}]", bucketName, s3Key, inputStream.GetHashCode(), inputStream.GetType());
            }
            return false;
        }

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool Upload(string filePathOnDisk, string bucketName, string s3Key)
        {
            return Upload(filePathOnDisk, bucketName, s3Key, false);
        }

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool Upload(string filePathOnDisk, string bucketName, string s3Key, bool trackProgress)
        {
            //Refenece URL: http://docs.aws.amazon.com/AmazonS3/latest/dev/HLTrackProgressMPUDotNet.html
            TransferUtilityUploadRequest tr = new TransferUtilityUploadRequest
            {
                BucketName = bucketName,
                FilePath = filePathOnDisk,
                Key = s3Key
            };
            try
            {
                using (TransferUtility ftu = new TransferUtility(_s3Client))
                {
                    if (trackProgress)
                    {
                        tr.UploadProgressEvent += new EventHandler<UploadProgressArgs>(uploadRequest_UploadPartProgressEvent);
                    }
                    ftu.Upload(tr);
                    if (trackProgress)
                    {
                        tr.UploadProgressEvent -= new EventHandler<UploadProgressArgs>(uploadRequest_UploadPartProgressEvent);
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Upload failed for Input File [Path:{0}]! Reference Bucket Name:{1} S3-Key:{2}!", filePathOnDisk.Length, bucketName, s3Key);
            }
            return false;
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool Upload(byte[] fileContent, string bucketName, string s3Key)
        {
            return Upload(fileContent, bucketName, s3Key, false);
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool Upload(byte[] fileContent, string bucketName, string s3Key, bool trackProgress)
        {
            if (fileContent == null || fileContent.Length <= 0)
            {
                FsLogManager.Fatal("Upload failed for Invalid File Content! Reference Bucket Name:{0} S3-Key:{1}!", bucketName, s3Key);
                throw new ArgumentException("Invalid 'File Content' provided for upload!");
            }

            try
            {
                using (Stream s = new MemoryStream(fileContent))
                {
                    TransferUtilityUploadRequest tr = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        InputStream = s,
                        Key = s3Key
                    };
                    using (TransferUtility ftu = new TransferUtility(_s3Client))
                    {
                        if (trackProgress)
                        {
                            tr.UploadProgressEvent += new EventHandler<UploadProgressArgs>(uploadRequest_UploadPartProgressEvent);
                        }

                        ftu.Upload(tr);

                        if (trackProgress)
                        {
                            tr.UploadProgressEvent -= new EventHandler<UploadProgressArgs>(uploadRequest_UploadPartProgressEvent);
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Upload failed for File Content [Length:{0}bytes]! Reference Bucket Name:{1} S3-Key:{2}!", fileContent.Length, bucketName, s3Key);
            }
            return false;
        }

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
        private void uploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
        {
            string content = string.IsNullOrEmpty(e.FilePath) ? "<Stream>" : string.Format("{0}", e.FilePath);
            FsLogManager.Info("Input:{0}; Upload Progress:{1}% [{2}/{3} bytes]", content, e.PercentDone, e.TransferredBytes, e.TotalBytes);
            if (_fileTransferProgressEvent != null)
            {
                _fileTransferProgressEvent(this, new FileTransferProgressArgs(FileTransferType.Upload, content, new TransferProgressArgs(e.PercentDone, e.TransferredBytes, e.TotalBytes)));
            }
        }
        #endregion
    }
}


using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FileSystemLib.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FileSystemLib
{
    internal sealed class S3Ops : Disposable, IS3OpsInternal
    {
        #region Static Members
        private static IFileTransferManagerAsync _fileTransferManagerAsync = null;
        private static object _locker = new object();
        #endregion

        #region Private Members
        private readonly string _fileChunkURLGeneratorService = null;
        private readonly string _fileChunkService = null;
        private readonly ISharedAppSettings _sharedAppSettings;
        private IAmazonS3 _s3Client;
        private EventHandler<FileTransferProgressArgs> _fileTransferProgressEvent;
        #endregion

        #region Constructor
        public S3Ops(ISharedAppSettings sharedAppSettings)
        {
            _sharedAppSettings = sharedAppSettings;
            //_fileChunkURLGeneratorService = string.Format("{0}/{1}", sharedAppSettings.FileChunkServiceEndpoint, sharedAppSettings.PreSignedURLGenerationCmd);
            //_fileChunkService = string.Format("{0}/{1}", sharedAppSettings.FileChunkServiceEndpoint, sharedAppSettings.FileChunkServiceGetChunkInfoCmd);
            //_s3Client = AWS_S3Client.GetS3Client(sharedAppSettings.AWSAccessKeys);
            _s3Client = new AmazonS3Client(sharedAppSettings.AWSAccessKeys.Value.Key, sharedAppSettings.AWSAccessKeys.Value.Value, RegionEndpoint.USWest1);

            Trace.TraceInformation("S3Ops instantiated with FileChunkURLGeneratorService:{0} & FileChunkService:{1}, Hashcode:{2}.", _fileChunkURLGeneratorService, _fileChunkService, GetHashCode());
        }
        #endregion

        #region IS3Ops

        #region Pre-Signed URL Related

        /// <summary>
        /// Generates Pre-Signed url for Downloading a file
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be downloaded, should contain file name along with file path</param>
        /// <param name="validity">Validity of the Pre-Signed url</param>
        /// <returns>Pre-Signed url if successful; null if failed</returns>
        public string GeneratePreSignedDownloadUrl(string bucketName, string s3Key, TimeSpan validity,string downloadFileName = null)
        {
            GetPreSignedUrlRequest r = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = s3Key,
                Expires = DateTime.Now.Add(validity)
            };

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                r.ResponseHeaderOverrides.ContentDisposition = "filename=\"" + downloadFileName + "\"";
            }
            try
            {
                string url = _s3Client.GetPreSignedURL(r);
                FsLogManager.Debug("Generate Pre-Signed Download Url for Bucket:{0}, S3Key:{1} [url:{2}]", bucketName, s3Key, url);

                return url;
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Failed to Generate Pre-Signed Download Url for Bucket:{0}, S3Key:{1}", bucketName, s3Key);
                return null;
            }
        }

        /// <summary>
        /// Generates Pre-Signed url for Downloading a file
        /// </summary>
        /// <param name="projectId">Project Id for which to generate Pre-Signed url</param>
        /// <param name="accountId">Account Id for which to generate Pre-Signed url</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="validity">Validity of the Pre-Signed url</param>
        /// <returns>Pre-Signed url if successful; null if failed</returns>
        public string GeneratePreSignedDownloadUrl(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, TimeSpan validity, string downloadFileName = null)
        {
            string bucketName, keyName;

            if (!getS3Info(projectId, accountId, fileNameInS3WithoutPath, locator, out bucketName, out keyName))
            {
                return null;
            }
            return GeneratePreSignedDownloadUrl(bucketName, keyName, validity, downloadFileName);
        }
        #endregion

        /// <summary>
        /// File Size of a given file
        /// </summary>
        /// <param name="projectId">Project Id in which the document exists</param>
        /// <param name="accountId">Account Id in which the document exists</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <returns>File Size of a given file (bytes) if successful; returns -VE value on failure</returns>
        public long GetFileSize(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator)
        {
            string bucketName, keyName;

            if (!getS3Info(projectId, accountId, fileNameInS3WithoutPath, locator, out bucketName, out keyName))
            {
                return -1;
            }
            return GetFileSize(bucketName, keyName);
        }

        /// <summary>
        /// File Size of a given file
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be downloaded, should contain file name along with file path</param>
        /// <returns>File Size of a given file if successful; returns -VE value on failure</returns>
        public long GetFileSize(string bucketName, string s3Key)
        {
            return getFileSizeEx(bucketName, s3Key, false);
        }

        /// <summary>
        /// Get Directory Content i.e. Files & Folders
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the folder to be queried</param>
        /// <returns>Directory Content i.e. Files & Folders if successful; else null</returns>
        public string[] GetDirectoryContent(string bucketName, string s3Key)
        {
            try
            {
                List<string> collection = new List<string>();
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketName;
                request.Prefix = s3Key;
                //request.MaxKeys = 2;
                do
                {
                    ListObjectsResponse response = _s3Client.ListObjects(request);

                    foreach (S3Object o in response.S3Objects)
                    {
                        FsLogManager.Debug("key:{0} size:{1}", o.Key, o.Size);
                        collection.Add(o.Key);
                    }

                    // If response is truncated, set the marker to get the next set of keys.
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);

                return collection.Count > 0 ? collection.ToArray() : null;
            }
            catch (AmazonS3Exception s3e)
            {
                if (s3e.ErrorCode != null && (s3e.ErrorCode.Equals("InvalidAccessKeyId") || s3e.ErrorCode.Equals("InvalidSecurity")))
                {
                    FsLogManager.Fatal(s3e, "Failed to get files from Bucket:{0} S3-Key:{1} due to invalid AWS credentials!", bucketName, s3Key);
                    return null;
                }
                FsLogManager.Fatal(s3e, "Failed to get files from Bucket:{0} S3-Key:{1}! Exception:{2}.", bucketName, s3Key, s3e.Message);
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Failed to get files from Bucket:{0} S3-Key:{1}! Exception:{2}.", bucketName, s3Key, e.Message);
                return null;
            }
            return null;
        }

        #region Upload Related

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be downloaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(string filePathOnDisk, string bucketName, string s3Key)
        {
            return UploadFile(filePathOnDisk, bucketName, s3Key, false);
        }

        /// <summary>
        /// Uploads a file from S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be downloaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(string filePathOnDisk, string bucketName, string s3Key, bool trackProgress)
        {
            FileUploadUtil u = new FileUploadUtil(_sharedAppSettings, _s3Client);

            u.FileTransferProgressEvent += OnFileTransferProgres;
            bool b = u.Upload(filePathOnDisk, bucketName, s3Key, trackProgress);
            u.FileTransferProgressEvent -= OnFileTransferProgres;

            return b;
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(byte[] fileContent, string bucketName, string s3Key)
        {
            return UploadFile(fileContent, bucketName, s3Key, false);
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(byte[] fileContent, string bucketName, string s3Key, bool trackProgress)
        {
            FileUploadUtil u = new FileUploadUtil(_sharedAppSettings, _s3Client);

            u.FileTransferProgressEvent += OnFileTransferProgres;
            bool b = u.Upload(fileContent, bucketName, s3Key, trackProgress);
            u.FileTransferProgressEvent -= OnFileTransferProgres;

            return b;
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(Stream inputStream, string bucketName, string s3Key)
        {
            return UploadFile(inputStream, bucketName, s3Key, false);
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <param name="bucketName">Bucket Name in which the file to be uploaded</param>
        /// <param name="s3Key">S3 Key of the file to be Uploaded, should contain file name along with file path</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(Stream inputStream, string bucketName, string s3Key, bool trackProgress)
        {
            FileUploadUtil u = new FileUploadUtil(_sharedAppSettings, _s3Client);

            u.FileTransferProgressEvent += OnFileTransferProgres;
            bool b = u.Upload(inputStream, bucketName, s3Key, trackProgress);
            u.FileTransferProgressEvent -= OnFileTransferProgres;

            return b;
        }

        /// <summary>
        /// Uploads a file from S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="filePathOnDisk">Fully qualified path name (FQPN) of the file in Disk or Network Share to upload</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string filePathOnDisk)
        {
            return UploadFile(projectId, accountId, fileNameInS3WithoutPath, locator, filePathOnDisk, false);
        }

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
        public bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string filePathOnDisk, bool trackProgress)
        {
            string bucketName, keyName;

            if (!getS3Info(projectId, accountId, fileNameInS3WithoutPath, locator, out bucketName, out keyName))
            {
                return false;
            }

            return UploadFile(filePathOnDisk, bucketName, keyName, trackProgress);
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="fileContent">Content of a file to upload</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, byte[] fileContent)
        {
            return UploadFile(projectId, accountId, fileNameInS3WithoutPath, locator, fileContent, false);
        }

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
        public bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, byte[] fileContent, bool trackProgress)
        {
            string bucketName, keyName;

            if (!getS3Info(projectId, accountId, fileNameInS3WithoutPath, locator, out bucketName, out keyName))
            {
                return false;
            }
            return UploadFile(fileContent, bucketName, keyName, trackProgress);
        }

        /// <summary>
        /// Uploads content of a file to S3 Synchronously.
        /// </summary>
        /// <param name="projectId">Project Id to which document needs to be uploaded</param>
        /// <param name="accountId">Account Id to which document needs to be uploaded</param>
        /// <param name="fileNameInS3WithoutPath">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="inputStream">Stream (File/Memory) to upload</param>
        /// <returns>true if successful; else false</returns>
        public bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, Stream inputStream)
        {
            return UploadFile(projectId, accountId, fileNameInS3WithoutPath, locator, inputStream, false);
        }

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
        public bool UploadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, Stream inputStream, bool trackProgress)
        {
            string bucketName, keyName;

            if (!getS3Info(projectId, accountId, fileNameInS3WithoutPath, locator, out bucketName, out keyName))
            {
                return false;
            }
            return UploadFile(inputStream, bucketName, keyName, trackProgress);
        }
        #endregion

        /// <summary>
        /// Deletes a file from S3.
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be deleted, should contain file name along with file path</param>
        /// <returns>true if successful; else false</returns>
        public bool DeleteFile(string bucketName, string s3Key)
        {
            FileResponseEntity fre = new FileResponseEntity();
            try
            {
                DeleteObjectRequest r = new DeleteObjectRequest()
                {
                    BucketName = bucketName,
                    Key = s3Key
                };
                DeleteObjectResponse response = _s3Client.DeleteObject(r);
                return fre.OperationStatus;
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Failed to delete file for Bucket Name:{0}; S3-Key:{1}, Exception:{2}!", bucketName, s3Key, e.Message);
                return false;
            }
        }

        public bool CreateFolder(string bucketName, string s3Key)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Copies file from one locattion to another in a given bucket
        /// </summary>
        /// <param name="bucketName">Bucket Name from which copy is to be performed(source bucket)</param>
        /// <param name="s3KeySource">S3 Key of the Source file to be copied</param>
        /// <param name="s3KeyDestination">S3 Key of the Destination file to be copied</param>
        /// /// <param name="destinationBucket">Bucket Name in which copy to be performed(destinationBucket)</param>
        /// <returns>true if successful; else false</returns>
        public bool CopyFile(string bucketName, string s3KeySource, string s3KeyDestination,string destinationBucket = null)
        {
            if (string.IsNullOrEmpty(destinationBucket))
            {
                destinationBucket = bucketName;
            }
            CopyObjectRequest r = new CopyObjectRequest()
            {
                SourceBucket = bucketName,
                SourceKey = s3KeySource,
                DestinationBucket = destinationBucket,
                DestinationKey = s3KeyDestination
            };

            try
            {
                CopyObjectResponse response = _s3Client.CopyObject(r);
                if (getFileSizeEx(destinationBucket, s3KeyDestination, true) > 0)
                {
                    return true;
                }
                FsLogManager.Fatal("Failed to copy file size [Bucket Name:{0} S3-Key-Source:{1} S3-Key-Destination:{2}] due to invalid AWS credentials!", bucketName, s3KeySource, s3KeyDestination);
                return false;
            }
            catch (AmazonS3Exception s3e)
            {
                if (s3e.ErrorCode != null && (s3e.ErrorCode.Equals("InvalidAccessKeyId") || s3e.ErrorCode.Equals("InvalidSecurity")))
                {
                    FsLogManager.Fatal(s3e, "Failed to copy file size [Bucket Name:{0} S3-Key-Source:{1} S3-Key-Destination:{2}] due to invalid AWS credentials!", bucketName, s3KeySource, s3KeyDestination);
                    return false;
                }
                FsLogManager.Fatal(s3e, "Failed to copy file size [Bucket Name:{0} S3-Key-Source:{1} S3-Key-Destination:{2}]!", bucketName, s3KeySource, s3KeyDestination);
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Failed to copy file size [Bucket Name:{0} S3-Key-Source:{1} S3-Key-Destination:{2}]!", bucketName, s3KeySource, s3KeyDestination);
                return false;
            }

            FsLogManager.Fatal("Control should never reach here! Failed to copy file size [Bucket Name:{0} S3-Key-Source:{1} S3-Key-Destination:{2}]!", bucketName, s3KeySource, s3KeyDestination);
            return false;
        }

        #region Download Related
        /// <summary>
        /// Downloads a file from S3 Synchronously. The file can either made of multiple physical Chunks which are not stitched yet or available as an integrated file.
        /// In the process of download, the following gets done.
        ///     a) Determines S3 locaion & check if exists. 
        ///     b) If not, contact HBase Service for availability (if queued for Stitching). 
        ///     c) If Queued, then download chunks, stitch, serve the file & then upload to S3 in a low priority queue.
        /// </summary>
        /// <param name="projectId">Project Id in which the document exists</param>
        /// <param name="accountId">Account Id in which the project exists</param>
        /// <param name="documentNameInS3">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="fileNameInS3WithoutPath">The Destination File Path In Local File System or Network Share</param>
        /// <returns>true if successful; else false</returns>
        public bool DownloadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string destinationFileInLocalFs)
        {
            return DownloadFile(projectId, accountId, fileNameInS3WithoutPath, locator, destinationFileInLocalFs, false);
        }

        /// <summary>
        /// Downloads a file from S3 Synchronously. The file can either made of multiple physical Chunks which are not stitched yet or available as an integrated file.
        /// In the process of download, the following gets done.
        ///     a) Determines S3 locaion & check if exists. 
        ///     b) If not, contact HBase Service for availability (if queued for Stitching). 
        ///     c) If Queued, then download chunks, stitch, serve the file & then upload to S3 in a low priority queue.
        /// </summary>
        /// <param name="projectId">Project Id in which the document exists</param>
        /// <param name="accountId">Account Id in which the project exists</param>
        /// <param name="documentNameInS3">Only the document/file name, without path</param>
        /// <param name="locator">Utility to determine Bucket & S3-Key. To be implemented by caller/host process</param>
        /// <param name="fileNameInS3WithoutPath">The Destination File Path In Local File System or Network Share</param>
        /// <returns>true if successful; else false</returns>
        public bool DownloadFile(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, string destinationFileInLocalFs, bool trackProgress)
        {
            string bucketName, keyName;

            if (!getS3Info(projectId, accountId, fileNameInS3WithoutPath, locator, out bucketName, out keyName))
            {
                return false;
            }

            long fileSize = getFileSizeEx(bucketName, keyName, true);
            if (fileSize > 0)
            {//File is available in S3, download now...
                if (!downloadFileFromS3Ex(bucketName, keyName, destinationFileInLocalFs, trackProgress))
                {
                    string s = string.Format("Failed to Download File [Source in S3:{0}; Bucket:{1}] to \"{2}\"! Reference Project Id:{3}, Account Id:{4}", bucketName, keyName, destinationFileInLocalFs, projectId, accountId);
                    FsLogManager.Fatal(s);
                    return false;
                }
                return true;
            }
            //   
            //File may not exist in S3 immediately after File Chunk uploads from Desktop File Sync. 
            //File is is being stitched by HBase Service... So download the file from this service itself
            //  Determine file chunk info (individual file in S3) from Hbase Service.
            //  Determine file chunk info (individual file in S3) from Hbase Service.
            //  Download each chunk
            //  Stitch All Chunks
            //  Deliver file
            //  Upload to S3 in Async manner
            //   

            FileChunkDownloader downloader = new FileChunkDownloader(_sharedAppSettings, _s3Client);
            KeyValuePair<string, string>? s3Detail = locator.GetBucketAndRootFolderPath(projectId, accountId);
            if (s3Detail != null)
            {
                bucketName = s3Detail.Value.Key;
                string folderInS3Bucket = s3Detail.Value.Value;

                return downloader.DownloadFile(accountId.ToString(), projectId.ToString(), fileNameInS3WithoutPath, bucketName, destinationFileInLocalFs, trackProgress);
            }
            return false;
        }

        /// <summary>
        /// Downloads a file from S3 Synchronously
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be deleted, should contain file name along with file path</param>
        /// <param name="targetPathOnDisk">The Destination File Path In Local File System or Network Share</param>
        /// <returns>true if successful; else false</returns>
        public bool DownloadFile(string bucketName, string s3Key, string targetPathOnDisk)
        {
            return DownloadFile(bucketName, s3Key, targetPathOnDisk, false);
        }

        /// <summary>
        /// Downloads a file from S3 Synchronously with ability to track progress.
        /// </summary>
        /// <param name="bucketName">Bucket Name in which the file is located</param>
        /// <param name="s3Key">S3 Key of the file to be deleted, should contain file name along with file path</param>
        /// <param name="targetPathOnDisk">The Destination File Path In Local File System or Network Share</param>
        /// <param name="trackProgress">Set true if progress needs to be tracked by caller. Caller needs to subscribe to event 'FileTransferProgressEvent'</param>
        /// <returns>true if successful; else false</returns>
        public bool DownloadFile(string bucketName, string s3Key, string targetPathOnDisk, bool trackProgress)
        {
            if (!downloadFileFromS3Ex(bucketName, s3Key, targetPathOnDisk, trackProgress))
            {
                string s = string.Format("Failed to Download File [Source in S3:{0}; Bucket:{1}] to \"{2}\"!", s3Key, bucketName, targetPathOnDisk);
                FsLogManager.Fatal(s);
                return false;
            }
            return true;
        }
        #endregion

        /// <summary>
        /// Transfers File in Async Manner
        /// </summary>
        public IFileTransferManagerAsync FileTransferManager
        {
            get
            {
                lock (_locker)
                {
                    if (_fileTransferManagerAsync == null)
                    {
                        _fileTransferManagerAsync = new FileTransferManagerAsync(this);
                    }
                    return _fileTransferManagerAsync;
                }
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
        private bool getS3Info(int projectId, int accountId, string fileNameInS3WithoutPath, IS3FileLocator locator, out string s3BucketName, out string s3KeyName)
        {
            s3BucketName = null;
            s3KeyName = null;
            if (locator == null)
            {
                throw new ArgumentException("Invalid 'IS3FileLocator' instance encountered!");
            }
            KeyValuePair<string, string>? s3Detail = locator.GetBucketAndFileLocation(projectId, accountId, fileNameInS3WithoutPath);

            if (s3Detail == null)
            {
                string s = string.Format("Failed to determine File Location for:{0} in AWS-S3! Reference Project Id:{1}, Account Id:{2}", fileNameInS3WithoutPath, projectId, accountId);
                FsLogManager.Fatal(s);
                return false;
            }
            s3BucketName = s3Detail.Value.Key;
            s3KeyName = s3Detail.Value.Value;
            return true;
        }

        private bool downloadFileFromS3Ex(string s3BucketName, string s3keyName, string destinationFileInLocalFs, bool trackProgress)
        {//
            try
            {
                TransferUtilityDownloadRequest tr = new TransferUtilityDownloadRequest
                {
                    BucketName = s3BucketName,
                    FilePath = destinationFileInLocalFs,
                    Key = s3keyName
                };

                using (TransferUtility ftu = new TransferUtility(_s3Client))
                {
                    FsLogManager.Debug("Downloading File:{0} from S3, Reference Bucket Name:{1} S3-Key:{2}!", destinationFileInLocalFs, s3BucketName, s3keyName);
                    if (trackProgress)
                    {
                        tr.WriteObjectProgressEvent += OnWriteObjectProgressEvent;
                    }
                    ftu.Download(tr);
                    if (trackProgress)
                    {
                        tr.WriteObjectProgressEvent -= OnWriteObjectProgressEvent;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Download failed for File:{0}! Reference Bucket Name:{1} S3-Key:{2}!", destinationFileInLocalFs, s3BucketName, s3keyName);
            }
            return false;
        }

        private long getFileSizeEx(string bucketName, string s3Key, bool isForCheckingFileExistence)
        {
            GetObjectMetadataRequest r = new GetObjectMetadataRequest()
            {
                BucketName = bucketName,
                Key = s3Key
            };

            try
            {
                GetObjectMetadataResponse response = _s3Client.GetObjectMetadata(r);
                return response.ContentLength;
            }
            catch (AmazonS3Exception s3e)
            {
                if (isForCheckingFileExistence)
                {
                    return -1;//No need to log errors!
                }
                if (s3e.ErrorCode != null && (s3e.ErrorCode.Equals("InvalidAccessKeyId") || s3e.ErrorCode.Equals("InvalidSecurity")))
                {
                    FsLogManager.Fatal(s3e, "Failed to get file size for Bucket Name:{0} S3-Key:{1} due to invalid AWS credentials!", bucketName, s3Key);
                    return -1;
                }
                FsLogManager.Fatal(s3e, "Failed to get file size for Bucket Name:{0} S3-Key:{1}! Exception:{2}.", bucketName, s3Key, s3e.Message);
            }
            catch (Exception e)
            {
                if (isForCheckingFileExistence)
                {
                    return -1;//No need to log errors!
                }

                FsLogManager.Fatal(e, "Failed to get file size for Bucket Name:{0} S3-Key:{1}! Exception:{2}.", bucketName, s3Key, e.Message);
                return -1;
            }
            if (isForCheckingFileExistence)
            {
                return -1;//No need to log errors!
            }

            FsLogManager.Fatal("Control should never reach here! Failed to get file size for Bucket Name:{0} S3-Key:{1}.", bucketName, s3Key);
            return -1;//Control should never reach here!
        }

        private void OnWriteObjectProgressEvent(object sender, WriteObjectProgressArgs e)
        {
            string content = string.IsNullOrEmpty(e.FilePath) ? "<Stream>" : string.Format("{0}", e.FilePath);
            FsLogManager.Info("Input:{0}; Download Progress:{1}% [{2}/{3} bytes]", content, e.PercentDone, e.TransferredBytes, e.TotalBytes);
            if (_fileTransferProgressEvent != null)
            {
                _fileTransferProgressEvent(this, new FileTransferProgressArgs(FileTransferType.Download, content, new TransferProgressArgs(e.PercentDone, e.TransferredBytes, e.TotalBytes)));
            }
        }

        private void OnFileTransferProgres(object sender, FileTransferProgressArgs e)
        {
            FsLogManager.Info("Input:{0}; Download Progress:{1}% [{2}/{3} bytes]", e.ObjectNameForTransfer, e.PercentDone, e.TransferredBytes, e.TotalBytes);
            if (_fileTransferProgressEvent != null)
            {
                _fileTransferProgressEvent(this, new FileTransferProgressArgs(FileTransferType.Upload, e.ObjectNameForTransfer, new TransferProgressArgs(e.PercentDone, e.TransferredBytes, e.TotalBytes)));
            }
        }

        protected override void doCleanup()
        {
            SafeDispose<IAmazonS3>(ref _s3Client);
        }
        #endregion

    }
}


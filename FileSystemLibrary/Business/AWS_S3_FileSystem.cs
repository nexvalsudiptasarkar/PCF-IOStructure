using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using FileSystemLib.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace FileSystemLib
{
    internal sealed class AwsS3FileSystem : FileSystemBase, IFileSystemLib
    {
        // Changes Made By Sudipta PutFile New Implementation for java using Put Request for less than 5 mb chunk 04-03-2013
        private static AmazonS3Client _s3Client;
        
        //"http://10.98.10.171/arcfilechunk/presign";
        private readonly string _fileChunkURLGeneratorService = null;   
     
        //"http://10.98.10.171/arcfilechunk/rest";
        private readonly string _fileChunkService = null;   

        #region Constructor
        public AwsS3FileSystem(ISharedAppSettings sharedAppSettings)
            : base(sharedAppSettings)
        {
            _fileChunkURLGeneratorService = string.Format("{0}/{1}", sharedAppSettings.FileChunkServiceEndpoint, sharedAppSettings.PreSignedURLGenerationCmd);
            _fileChunkService = string.Format("{0}/{1}", sharedAppSettings.FileChunkServiceEndpoint, sharedAppSettings.FileChunkServiceGetChunkInfoCmd);

            _s3Client = AWS_S3Client.GetS3Client(AwsAccessKeys);

            Trace.TraceInformation("AwsS3FileSystem fs instantiated with FileChunkURLGeneratorService:{0} & FileChunkService:{1}, Hashcode:{2}.", _fileChunkURLGeneratorService, _fileChunkService, GetHashCode());
        }
        #endregion PutFile

        # region PutFile

        public string generateUploadID(string folderName, string keyName)
        {
            InitiateMultipartUploadResponse imUploadRes = null;
            string uploadKeyName = folderName + Slash.FrontSlash.GetDescription() + keyName;
            try
            {
                InitiateMultipartUploadRequest imUploadReq = new InitiateMultipartUploadRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Key = (uploadKeyName),
                    ServerSideEncryptionMethod = (ServerSideEncryptionMethod.AES256)
                };

                imUploadRes = _s3Client.InitiateMultipartUpload(imUploadReq);

            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "AwsS3FileSystem: Failed generate Upload ID for Folder:{0}, Upload Key:{1}!", folderName, keyName);
                throw;
            }
            return imUploadRes.UploadId;
        }

        //new for file chunk
        public string generateUploadID(string folderName, string keyName, string accountId)
        {
            InitiateMultipartUploadResponse imUploadRes = null;
            string uploadKeyName = folderName + Slash.FrontSlash.GetDescription() + keyName;
            try
            {
                InitiateMultipartUploadRequest imUploadReq = new InitiateMultipartUploadRequest()
                {
                    BucketName = (accountId),
                    Key = (uploadKeyName),
                    ServerSideEncryptionMethod = (ServerSideEncryptionMethod.AES256)
                };

                imUploadRes = _s3Client.InitiateMultipartUpload(imUploadReq);

            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "AwsS3FileSystem: Failed generate Upload ID for Folder:{0}, Upload Key:{1}, Bucket: {2}!", folderName, keyName, accountId);
                throw e;
            }
            return imUploadRes.UploadId;
        }

        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length)
        {
            Stream ipstream = fileReqEntity.Stream;
            string keyName = fileReqEntity.OriginalName;
            string FolderName = fileReqEntity.S3Entity.FolderLocation;
            string UploadID = fileReqEntity.S3Entity.UploadID;
            int PartNo = fileReqEntity.S3Entity.PartNo;
            string uploadKeyName = FolderName + Slash.FrontSlash.GetDescription() + keyName;
            FileResponseEntity response = new FileResponseEntity();
            try
            {
                UploadPartRequest uploadRequest = new UploadPartRequest()
                    {
                        BucketName = (AccountNeutralS3BucketName),
                        Key = (uploadKeyName),
                        FilePosition = (Offset),
                        PartNumber = (PartNo),
                        PartSize = (Length),
                        UploadId = (UploadID),
                        Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut)
                    };
                //.WithServerSideEncryptionMethod(ServerSideEncryptionMethod.AES256);
                uploadRequest.Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut);
                uploadRequest.InputStream = ipstream;
                uploadRequest.ReadWriteTimeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut);
                UploadPartResponse partres = _s3Client.UploadPart(uploadRequest);
                response.ETag = partres.ETag;
                response.PartNumber = partres.PartNumber;
                response.ByteWritten = ipstream.Length;
                if (partres.HttpStatusCode == HttpStatusCode.OK) response.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem: Failed PUT file, Upload Key:{0}, File:{1}, Offset:{2}, Length:{3}!", fileReqEntity.OriginalName, keyName, Offset, Length);
                throw;
            }
            return response;

        }

        /// <summary>
        /// new method for file-chunk project,uploads files to s3 chunk-wise,  to be used from cache-Manager
        /// only difference between previous and this is the new parameter accountID
        /// </summary>
        /// <param name="fileReqEntity">req entity</param>
        /// <param name="Offset">offset for chunk</param>
        /// <param name="Length">length of chunk</param>
        /// <param name="accountId">will be used as S3 bucker</param>
        /// <returns></returns>
        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, string s3Bucket)
        {
            Stream ipstream = fileReqEntity.Stream;
            string keyName = fileReqEntity.OriginalName;
            string FolderName = fileReqEntity.S3Entity.FolderLocation;
            string UploadID = fileReqEntity.S3Entity.UploadID;
            int PartNo = fileReqEntity.S3Entity.PartNo;
            string uploadKeyName = FolderName + Slash.FrontSlash.GetDescription() + keyName;
            FileResponseEntity response = new FileResponseEntity();
            try
            {
                UploadPartRequest uploadRequest = new UploadPartRequest()
                {
                    BucketName = (s3Bucket),
                    Key = (uploadKeyName),
                    FilePosition = (Offset),
                    PartNumber = (PartNo),
                    PartSize = (Length),
                    UploadId = (UploadID),
                    Timeout = new TimeSpan(0, 0, 0, 0, (FileSystemTimeOut))
                };
                //.WithServerSideEncryptionMethod(ServerSideEncryptionMethod.AES256);
                uploadRequest.Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut);
                uploadRequest.InputStream = ipstream;
                uploadRequest.ReadWriteTimeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut);
                UploadPartResponse partres = _s3Client.UploadPart(uploadRequest);
                response.ETag = partres.ETag;
                response.PartNumber = partres.PartNumber;
                response.ByteWritten = ipstream.Length;
                if (partres.HttpStatusCode == HttpStatusCode.OK) response.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem: Failed PUT file, Upload Key:{0}, File:{1}, Offset:{2}, Length:{3}, Bucket:{4}!", fileReqEntity.OriginalName, keyName, Offset, Length, s3Bucket);
                throw ex;
            }
            return response;
        }

        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, bool DirectUpload)
        {
            Stream ipstream = fileReqEntity.Stream;
            string keyName = fileReqEntity.OriginalName;
            string FolderName = fileReqEntity.S3Entity.FolderLocation;
            string UploadID = fileReqEntity.S3Entity.UploadID;
            int PartNo = fileReqEntity.S3Entity.PartNo;
            string uploadKeyName = FolderName + Slash.FrontSlash.GetDescription() + keyName;
            FileResponseEntity responseObj = new FileResponseEntity();
            try
            {
                PutObjectRequest Request = new PutObjectRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Key = (uploadKeyName),
                    Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut),
                    ServerSideEncryptionMethod = (ServerSideEncryptionMethod.AES256)
                };
                Request.InputStream = (fileReqEntity.Stream);
                PutObjectResponse response = _s3Client.PutObject(Request);
                responseObj = new FileResponseEntity();
                if (response.HttpStatusCode == HttpStatusCode.OK) responseObj.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem: Failed PUT file, Upload Key:{0}, File:{1}, Offset:{2}, Length:{3}, DirectUpload?:{4}!", keyName, fileReqEntity.OriginalName, Offset, Length, DirectUpload);
                throw;
            }
            return responseObj;
        }

        public FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETag)
        {
            FileResponseEntity ientity = null;
            string uploadKeyName = folderName + Slash.FrontSlash.GetDescription() + keyName;
            try
            {
                List<PartETag> partETags = new List<PartETag>();
                foreach (DataRow iRow in partETag.Tables[0].Rows)
                {
                    partETags.Add(new PartETag(Convert.ToInt32(iRow["ChunkNumber"].ToString()), iRow["ETagID"].ToString()));
                }
                CompleteMultipartUploadRequest compRequest = new CompleteMultipartUploadRequest();
                compRequest.BucketName = AccountNeutralS3BucketName;
                compRequest.Key = uploadKeyName;
                compRequest.UploadId = UploadID;
                compRequest.PartETags = partETags;
                CompleteMultipartUploadResponse compRes = _s3Client.CompleteMultipartUpload(compRequest);
                if (compRes != null && compRes.HttpStatusCode == HttpStatusCode.OK)
                {
                    ientity = new FileResponseEntity();
                    ientity.OperationStatus = true;
                }
                else AbortFailedRequest(UploadID, uploadKeyName);
            }
            catch (Exception e)
            {
                AbortFailedRequest(UploadID, uploadKeyName);
                FsLogManager.Fatal(e, "AwsS3FileSystem.CompleteMultiPartUploadS3 Failed! UploadKeyName:{0}!", uploadKeyName);
                throw;
            }
            return ientity;
        }

        //new for file chunk
        public FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETag, string s3Bucket)
        {
            FileResponseEntity ientity = null;
            string uploadKeyName = folderName + Slash.FrontSlash.GetDescription() + keyName;
            try
            {
                List<PartETag> partETags = new List<PartETag>();
                foreach (DataRow iRow in partETag.Tables[0].Rows)
                {
                    partETags.Add(new PartETag(Convert.ToInt32(iRow["ChunkNumber"].ToString()), iRow["ETagID"].ToString()));
                }
                CompleteMultipartUploadRequest compRequest = new CompleteMultipartUploadRequest();
                compRequest.BucketName = s3Bucket;
                compRequest.Key = uploadKeyName;
                compRequest.UploadId = UploadID;
                compRequest.PartETags = partETags;
                CompleteMultipartUploadResponse compRes = _s3Client.CompleteMultipartUpload(compRequest);
                if (compRes != null && compRes.HttpStatusCode == HttpStatusCode.OK)
                {
                    ientity = new FileResponseEntity();
                    ientity.OperationStatus = true;
                }
                else AbortFailedRequest(UploadID, uploadKeyName);

            }
            catch (Exception e)
            {
                AbortFailedRequest(UploadID, uploadKeyName);
                FsLogManager.Fatal(e, "AwsS3FileSystem.CompleteMultiPartUploadS3 Failed! UploadKeyName:{0}, Bucket:{1}!", uploadKeyName, s3Bucket);
                throw e;
            }
            return ientity;
        }

        /// <summary>
        /// Put file for small file size
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="Location"> Nas Location </param>
        /// <param name="originalName"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite)
        {
            FileResponseEntity fileResEntiry = null;
            string uploadKeyName = destinationLocation + Slash.FrontSlash.GetDescription() + originalName;
            try
            {
                Stream stream = File.Open(sourceFileLocation + Slash.FrontSlash.GetDescription() + originalName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                PutObjectRequest Request = new PutObjectRequest()
                {
                    BucketName = AccountNeutralS3BucketName,
                    Key = uploadKeyName,
                    Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut),
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };
                Request.InputStream = stream;
                PutObjectResponse response = _s3Client.PutObject(Request);
                fileResEntiry = new FileResponseEntity();
                if (response.HttpStatusCode == HttpStatusCode.OK)
                    fileResEntiry.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem: PutFile Failed! UploadKeyName:{0}, SourceFileLocation:{1}, OriginalName:{2}!", uploadKeyName, sourceFileLocation, originalName);
                throw;
            }

            return fileResEntiry;
        }

        public FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite, string s3Bucket)
        {
            FileResponseEntity fileResEntiry = null;
            string uploadKeyName = destinationLocation + Slash.FrontSlash.GetDescription() + originalName;
            try
            {
                Stream stream = File.Open(sourceFileLocation + Slash.FrontSlash.GetDescription() + originalName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                PutObjectRequest Request = new PutObjectRequest()
                {
                    BucketName = s3Bucket,
                    Key = uploadKeyName,
                    Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut),
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };
                Request.InputStream = stream;
                PutObjectResponse response = _s3Client.PutObject(Request);
                fileResEntiry = new FileResponseEntity();
                if (response.HttpStatusCode == HttpStatusCode.OK)
                    fileResEntiry.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem: PutFile Failed! UploadKeyName:{0}, SourceFileLocation:{1}, OriginalName:{2}!", uploadKeyName, sourceFileLocation, originalName);
                throw;
            }

            return fileResEntiry;
        }

        private AbortMultipartUploadResponse AbortFailedRequest(string UploadID, string uploadKeyName)
        {
            AbortMultipartUploadRequest abortRequest = new AbortMultipartUploadRequest()
            {
                BucketName = AccountNeutralS3BucketName,
                UploadId = UploadID,
                Key = uploadKeyName
            };
            AbortMultipartUploadResponse iresponse = _s3Client.AbortMultipartUpload(abortRequest);
            return iresponse;
        }


        # endregion PutFile

        #region GetFile
        public int FolderWiseItemInfo(string folderName)
        {
            S3DirectoryInfo ret = null;
            try
            {
                if (!String.IsNullOrEmpty(AccountNeutralS3BucketName) && !String.IsNullOrEmpty(folderName))
                {
                    int last = folderName.LastIndexOf('\\');
                    int secondlast = folderName.LastIndexOf('\\', last - 1);
                    ret = new S3DirectoryInfo(_s3Client, AccountNeutralS3BucketName, folderName.Substring(0, secondlast));
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.FolderWiseItemInfo Failed! folderName:{0}!", folderName);
                throw;
            }
            return ret.GetFiles().Count();
        }

        public string GetObjectDetails(string keyName)
        {
            string ContentLength = string.Empty;
            try
            {
                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = AccountNeutralS3BucketName,
                    Key = keyName
                };
                GetObjectMetadataResponse response = _s3Client.GetObjectMetadata(request);
                ContentLength = response.ContentLength.ToString();
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetObjectDetails Failed! keyName:{0}!", keyName);
                throw;
            }
            return ContentLength;
        }

        public long GetFileContentLength(string fileLocation, string fileName)
        {
            string keyName = fileLocation + Slash.FrontSlash.GetDescription() + fileName;
            try
            {
                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = AccountNeutralS3BucketName,
                    Key = keyName
                };
                GetObjectMetadataResponse response = _s3Client.GetObjectMetadata(request);
                return response.ContentLength;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetFileContentLength Failed! keyName:{0}!", keyName);
                throw;
            }
        }

        public FileResponseEntity GetFile(FileRequestEntity chunkedReqEntity, long offset, long length)
        {
            HttpContext context = chunkedReqEntity.httpContext;
            string Location = chunkedReqEntity.S3Entity.FolderLocation;
            string DocumentName = chunkedReqEntity.OriginalName;
            if (string.IsNullOrEmpty(Location) || string.IsNullOrEmpty(DocumentName))
            {
                throw new Exception("Wrong params sent to GetFile(FileRequestEntity chunkedReqEntity, long offset, long length): " + Location + ", " + DocumentName);
            }

            FileResponseEntity fileResponse = null;
            Stream stream = null;
            byte[] buffer = new byte[DownloadBufferSize];
            long bytesProcessed = default(long);
            string keyName = Location + Slash.FrontSlash.GetDescription() + DocumentName;

            try
            {
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Key = (keyName),
                    ByteRange = new ByteRange((long)offset, (long)(offset + length)),
                };
                GetObjectResponse response = _s3Client.GetObject(request);
                stream = response.ResponseStream;
                int bytesRead = default(int);
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bytesProcessed += bytesRead;
                    context.Response.OutputStream.Write(buffer, 0, bytesRead);
                    context.Response.Flush();
                }

                if (response != null)
                {
                    fileResponse = new FileResponseEntity();
                    fileResponse.CompletedByte = bytesProcessed;
                    fileResponse.ContentLength = bytesProcessed;
                    fileResponse.OperationStatus = true;
                }
                return fileResponse;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetFile Failed! keyName:{0}, bytes Processed:{1}!", keyName, bytesProcessed);
                throw;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        public FileResponseEntity GetFile(FileRequestEntity fileReqEntity)
        {
            HttpContext context = fileReqEntity.httpContext;
            string Location = fileReqEntity.S3Entity.FolderLocation;
            string DocumentName = fileReqEntity.OriginalName;

            if (string.IsNullOrEmpty(Location) || string.IsNullOrEmpty(DocumentName))
            {
                throw new Exception("Wrong params sent to GetFile(FileRequestEntity fileReqEntity): " + Location + ", " + DocumentName);
            }

            FileResponseEntity fileResponse = null;
            Stream stream = null;
            byte[] buffer = new byte[DownloadBufferSize];
            long bytesProcessed = default(long);
            string keyName = Location + Slash.FrontSlash.GetDescription() + DocumentName;

            try
            {
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = AccountNeutralS3BucketName,
                    Key = (keyName),
                };
                GetObjectResponse response = _s3Client.GetObject(request);
                stream = response.ResponseStream;
                string length = response.ContentLength.ToString(); //GetFileContentLength(BucketName, keyName);
                context.Response.AddHeader("Content-Length", length);

                int bytesRead = 0;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bytesProcessed += bytesRead;
                    context.Response.OutputStream.Write(buffer, 0, bytesRead);
                    context.Response.Flush();
                }

                if (response != null)
                {
                    fileResponse = new FileResponseEntity();
                    fileResponse.CompletedByte = bytesProcessed;
                    fileResponse.ContentLength = bytesProcessed;
                    fileResponse.OperationStatus = true;
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetFile Failed! keyName:{0}, bytes Processed:{1}!", keyName, bytesProcessed);
                throw;
            }
            finally
            {
                if (stream != null) stream.Close();
            }
            return fileResponse;
        }

        public Stream GetFile(string Location, string DocumentName, ref WebRequest request, ref WebResponse response, IDictionary<string, object> _dictionary)
        {
            if (string.IsNullOrEmpty(Location) || string.IsNullOrEmpty(DocumentName))
            {
                throw new Exception("Wrong params sent to GetFile(string Location, string DocumentName, ref WebRequest request, ref WebResponse response,IDictionary <string,object>_dictionary): " + Location + ", " + DocumentName);
            }
            string keyName = Location + Slash.FrontSlash.GetDescription() + DocumentName;
            try
            {

                GetObjectRequest getRequest = new GetObjectRequest()
                {
                    BucketName = AccountNeutralS3BucketName,
                    Key = (keyName),
                };
                GetObjectResponse getResponse = _s3Client.GetObject(getRequest);
                ///Mand Code: Need to keep live the response for caller 
                _dictionary.Add("fixForResponseGettingDisposed", getResponse);

                return getResponse.ResponseStream;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetFile Failed! DocumentName:{0}, keyName:{1}!", DocumentName, keyName);
                throw;
            }
        }
        /// <summary>
        /// Downloads a file from S3 to Local File System
        /// </summary>
        /// <param name="sourceLocationInS3"></param>
        /// <param name="sourceDocumentNameInS3"></param>
        /// <param name="destinationFileInLocalFs"></param>
        /// <returns></returns>
        /// <remarks>Atanu Banik 12-Jun-2015: The abstraction is already broken, NEED TO REDESIGN THIS CLASS LIBRARY! Adding 1 more method which will make this worse!</remarks>
        public bool SaveFile(string sourceLocationInS3, string sourceDocumentNameInS3, string destinationFileInLocalFs)
        {
            if (string.IsNullOrEmpty(sourceLocationInS3) || string.IsNullOrEmpty(sourceDocumentNameInS3))
            {
                throw new ArgumentException("Wrong params sent to SaveFile");
            }
            string keyName = string.Format("{0}/{1}", sourceLocationInS3, sourceDocumentNameInS3);
            try
            {
                GetObjectRequest getRequest = new GetObjectRequest()
                {
                    BucketName = AccountNeutralS3BucketName,
                    Key = (keyName),
                };
                using (GetObjectResponse getResponse = _s3Client.GetObject(getRequest))
                {
                    getResponse.WriteResponseStreamToFile(destinationFileInLocalFs, false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.SaveFile Failed! Location:{0}: DocumentName:{1}, Destination File:{2}!", sourceLocationInS3, sourceDocumentNameInS3, destinationFileInLocalFs);
                Console.WriteLine("{0}! Exception:{1}", ex.Message, ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public FileResponseEntity GetFile(string DocumentName, string accountId, string containerId, HttpContext httpcontext)
        {
            HttpContext context = httpcontext;

            if (string.IsNullOrEmpty(DocumentName))
            {
                throw new Exception("Wrong params sent to GetFile(FileRequestEntity fileReqEntity): " + ", " + DocumentName);
            }

            FileResponseEntity fileResponse = null;
            Stream stream = null;
            byte[] buffer = new byte[DownloadBufferSize];
            long bytesProcessed = default(long);

            try
            {
                stream = GetFile(DocumentName, accountId, containerId);
                stream.Position = 0;
                string length = stream.Length.ToString(); //GetFileContentLength(BucketName, keyName);
                context.Response.AddHeader("Content-Length", length);

                int bytesRead = default(int);
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bytesProcessed += bytesRead;
                    context.Response.OutputStream.Write(buffer, 0, bytesRead);
                    context.Response.Flush();
                }

                if (stream != null)
                {
                    fileResponse = new FileResponseEntity();
                    fileResponse.CompletedByte = bytesProcessed;
                    fileResponse.ContentLength = bytesProcessed;
                    fileResponse.OperationStatus = true;
                }
            }
            catch (Exception ex)
            {
                //LogManager.Trace("FileSystem.GetFile(FileRequestEntity fileReqEntity)", ex, "keyName = " + keyName + " : bytesProcessed = " + bytesProcessed);
                return null;
            }
            finally
            {
                if (stream != null) stream.Close();
            }

            return fileResponse;
        }

        //new method for file chunk
        public bool GetFileAndSave(string saveLocation, string DocumentName, string accountId, string containerId)
        {
            bool isFileSuccessfullySaved = false;

            if (string.IsNullOrEmpty(DocumentName) || (string.IsNullOrEmpty(saveLocation)))
            {
                throw new Exception("Wrong params sent to GetFile(string DocumentName, ref WebRequest request, ref WebResponse response,IDictionary <string,object>_dictionary):  " + DocumentName);
            }

            string responseContent = string.Empty;
            string fileName = DocumentName;
            try
            {
                responseContent = GetPresignedURL(fileName, accountId, containerId);
                //check if this url is valid
                if (!string.IsNullOrEmpty(responseContent))
                {
                    try
                    {
                        var client = new WebClient();
                        client.DownloadFile(responseContent, saveLocation);
                        isFileSuccessfullySaved = true;
                    }
                    catch (Exception e)
                    {
                        return isFileSuccessfullySaved;
                    }
                    return isFileSuccessfullySaved;
                }
                else
                {
                    isFileSuccessfullySaved = StitchChunksToFile(saveLocation, DocumentName, accountId, containerId);
                    return isFileSuccessfullySaved;
                }
            }
            catch (WebException webEx)
            {
                FsLogManager.Fatal(string.Format("Failed to get file for Document: {0} in account: {1} for projectId : {2}. Message : {3}", DocumentName, accountId, containerId, webEx.Message));
                return isFileSuccessfullySaved;
            }
        }

        //new method for file chunk
        public Stream GetFile(string DocumentName, string accountId, string containerId)
        {
            if (string.IsNullOrEmpty(DocumentName))
            {
                throw new Exception("Wrong params sent to GetFile(string DocumentName, ref WebRequest request, ref WebResponse response,IDictionary <string,object>_dictionary):  " + DocumentName);
            }

            string responseContent = string.Empty;
            string fileName = DocumentName;
            string tempFilePath = Path.GetTempPath();
            tempFilePath += @"//" + DocumentName;
            try
            {
                responseContent = GetPresignedURL(fileName, accountId, containerId);
                //check if this url is valid
                if (!string.IsNullOrEmpty(responseContent))
                {
                    HttpWebRequest requestToValidateURL = WebRequest.Create(responseContent) as HttpWebRequest;
                    requestToValidateURL.Method = "GET";
                    using (var datastream = requestToValidateURL.GetResponse().GetResponseStream())
                    {
                        MemoryStream stream = new MemoryStream();
                        CopyStream(datastream, stream);

                        return stream;
                    }
                }
                else
                {
                    bool isFileStichedCompletely = StitchChunksToFile(tempFilePath, DocumentName, accountId, containerId);
                    if (isFileStichedCompletely)
                    {
                        MemoryStream stream = new MemoryStream();
                        //read file from saved location, write it to memory stream
                        using (FileStream fileStream = File.OpenRead(tempFilePath))
                        {
                            stream.SetLength(fileStream.Length);
                            fileStream.Read(stream.GetBuffer(), 0, (int)fileStream.Length);
                        }
                        return stream;
                    }
                    return null;
                }
            }
            catch (WebException webEx)
            {
                FsLogManager.Fatal(string.Format("Failed to get file for Document: {0} in account: {1} for projectId : {2}. Message : {3}", DocumentName, accountId, containerId, webEx.Message));
                return null;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);

            }
        }

        //new method for file chunk
        private string GetPresignedURLForChunks(string fileName, string accountId, string projectId)
        {
            string url = string.Empty;
            string chunksFolder = "chunks";
            try
            {
                url = GetPresignedURL(fileName, accountId, chunksFolder);//s3Client.GetPreSignedURL(request);
                FsLogManager.Info(string.Format("URL generated for {0} in {1} for {2} is {3}", fileName, accountId, projectId, url));
                return url;
            }
            catch (Exception ex)
            {
                FsLogManager.Info(string.Format("Exception : {0} for fileName : {1}, accountId: {2}, projectId: {3}", ex.Message, fileName, accountId, projectId));
                FsLogManager.Fatal(ex, "AWS_S3_FileSystem.GetFilePreSignedUrl(string fileName, long accountId, long projectId)", ex, "keyName = " + projectId.ToString() + "/" + fileName + ex.StackTrace);
                return string.Empty;
            }
        }

        //new method for file chunk
        private string GetPresignedURL(string fileName, string accountId, string projectId)
        {
            string responseContent = string.Empty;
            var httpRequest = (HttpWebRequest)WebRequest.Create(_fileChunkURLGeneratorService);

            var requestHeaders = new Dictionary<string, string>();
            requestHeaders.Add("Mimetype", "text/*");
            httpRequest.ContentType = @"application/x-www-form-urlencoded";
            foreach (var header in requestHeaders)
            {
                httpRequest.Headers.Set(header.Key, header.Value);
            }

            httpRequest.Method = "POST";

            string body = "fileName=" + fileName + "&urlType=" + "DOWNLOAD" + "&accountId=" + accountId + "&folderName=" + projectId + "&logicalFileName=" + "";

            if (string.IsNullOrEmpty(body))
            {
                httpRequest.ContentLength = 0;
            }
            else
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
                httpRequest.ContentLength = bytes.Length;
                System.IO.Stream requestStream = httpRequest.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
            }

            using (var datastream = httpRequest.GetResponse().GetResponseStream())
            {
                using (var reader = new System.IO.StreamReader(datastream))
                {
                    responseContent = reader.ReadToEnd();//this will now contain URL for the file
                }
            }
            return responseContent;
        }

        /// <summary>
        /// writes chunks to temporary file
        /// </summary>
        /// <param name="downloadUrls">list of urls which are to downloaded</param>
        /// <param name="tempFile">temporary file path, where chunks are written</param>
        /// <param name="uncompletedUrls">if all chunks are not downloaded, this contains urls which should be retried, else this will be empty</param>
        /// <returns>boolean value which indicate whether all chunks were successfully downloaded and written to temporary file</returns>

        private bool writeChunksToTemporaryFile(List<string> downloadUrls, string tempFile, out List<string> uncompletedUrls)
        {
            bool fullyCompleted = true;
            byte[] MyBuffer = new byte[1024 * 1024];
            uncompletedUrls = new List<string>(downloadUrls);
            int BytesRead;
            try
            {
                foreach (string URL in downloadUrls)
                {
                    FsLogManager.Info(string.Format("Started executing url : {0}", URL));
                    WebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
                    using (WebResponse response = request.GetResponse())
                    {
                        using (FileStream MyFileStream = new FileStream(tempFile, FileMode.Append, FileAccess.Write))
                        {
                            // Read the chunk of the web response into the buffer
                            while ((BytesRead = response.GetResponseStream().Read(MyBuffer, 0, MyBuffer.Length)) > 0)
                            {
                                // Write the chunk from the buffer to the file
                                MyFileStream.Write(MyBuffer, 0, BytesRead);
                            }
                        }
                    }
                    FsLogManager.Info("   completed!!");
                    uncompletedUrls.Remove(URL);
                }
                return fullyCompleted;
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(string.Format("Download failed for above url : {0}", e.Message));
                fullyCompleted = false;
                return fullyCompleted;
            }
        }

        /// <summary>
        /// will be called only for files uploaded through sync, hence s3 file name will like "FileId_RevisionId"
        /// </summary>
        /// <param name="Location">not used</param>
        /// <param name="DocumentName">s3 file name</param>
        /// <param name="accountId">s3 bucket name</param>
        /// <param name="containerId">folder inside s3 bucket</param>
        /// <returns></returns>
        private bool StitchChunksToFile(string saveLocaton, string DocumentName, string accountId, string containerId)
        {
            FsLogManager.Info(string.Format("Started stitch for Document Name : {0}, accountID: {1}, containerId: {2}", DocumentName, accountId, containerId));
            int retryCount = 0;
            List<string> DownloadURLsForChunks = new List<string>();
            List<string> uncompletedDownloadURLsForChunks = new List<string>();
            bool success = false;
            try
            {
                //get all chunks for that particular file; generate download url for them ; then stitch them, in order and then finally send the resultant stream 
                if (!Path.GetFileNameWithoutExtension(DocumentName).Contains("_"))
                {
                    return success;
                }
                string[] IDs = Path.GetFileNameWithoutExtension(DocumentName).Split('_');
                string fileId = IDs[0];
                string revisionId = IDs[1];
                List<ChunkMetaResponse> chunks = GetChunksInFile(accountId, fileId, revisionId, containerId);
                FsLogManager.Info(string.Format("Received # {0} chunks", chunks.Count()));
                FsLogManager.Info(string.Format("Temporary file used is at {0}", saveLocaton));
                if (chunks != null)
                {
                    string url = string.Empty;
                    foreach (ChunkMetaResponse chunk in chunks)
                    {
                        url = string.Empty;
                        url = GetPresignedURLForChunks(chunk.chunkHeader + "_" + chunk.chunkHash, accountId, containerId);
                        DownloadURLsForChunks.Add(url);
                    }
                }
                if (DownloadURLsForChunks.Count > 0)
                {

                    success = writeChunksToTemporaryFile(DownloadURLsForChunks, saveLocaton, out uncompletedDownloadURLsForChunks);

                    if (success)
                    {
                        return success;
                    }
                    else
                    {
                        while (retryCount <= 3)
                        {
                            if (success)
                            {
                                return success;
                            }
                            else
                            {
                                DownloadURLsForChunks = uncompletedDownloadURLsForChunks;
                                FsLogManager.Info(string.Format("stiching failed mid-way, will try again for # {0} chunks", DownloadURLsForChunks.Count()));
                                uncompletedDownloadURLsForChunks = new List<string>();
                                success = writeChunksToTemporaryFile(DownloadURLsForChunks, saveLocaton, out uncompletedDownloadURLsForChunks);
                                retryCount++;
                            }
                        }

                        if (retryCount > 3)
                            success = false;
                    }
                }
                success = false;
                return success;
            }
            catch (Exception e)
            {
                if (DocumentName.Contains('_'))
                    FsLogManager.Fatal(string.Format("Failed at location: {0} for document: {1}, in account: {2}!! Reason" + e.Message, saveLocaton, DocumentName, accountId));
                success = false;
                return success;
            }
        }

        //new method for file chunk
        /// <summary>
        /// copies file stream to a memory stream
        /// </summary>
        /// <param name="input">filestream for each chunk</param>
        /// <param name="output">combined memorystream for all chunks</param>
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        //new method for file chunk
        public List<ChunkMetaResponse> GetChunksInFile(string accountId, string fileId, string revision, string projectId)
        {
            StringBuilder queryBody = new StringBuilder();
            String action = "GET_FILE_CHUNKS";
            queryBody.Append('{')
                .Append('"').Append("action").Append('"').Append(':')
                .Append('"').Append(action).Append('"').Append(',')
                .Append("accountId").Append(':').Append(accountId).Append(',')
                .Append("projectId").Append(':').Append(projectId).Append(',')
                .Append("fileId").Append(':').Append(fileId).Append(',')
                .Append("revision").Append(':').Append(revision)
            .Append('}');
            string responseBody = getResponse(queryBody.ToString());

            if (!string.IsNullOrEmpty(responseBody))
            {
                GeneralStringResponseList response = Newtonsoft.Json.JsonConvert.DeserializeObject<GeneralStringResponseList>(responseBody);
                Newtonsoft.Json.Linq.JArray q = Newtonsoft.Json.Linq.JArray.Parse(response.values.First().chunksJson);
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(q);
                ChunkMetaResponse[] chunkOffsetJsonList = Newtonsoft.Json.JsonConvert.DeserializeObject<ChunkMetaResponse[]>(jsonString);
                return chunkOffsetJsonList.ToList();
            }
            else
            {
                return new List<ChunkMetaResponse>();
            }
        }


        //new method for file chunk
        private string getResponse(string queryBody)
        {
            try
            {
                string body = string.Empty;
                var httpRequest = (HttpWebRequest)WebRequest.Create(_fileChunkService);
                string responseContent = string.Empty;

                var requestHeaders = new Dictionary<string, string>();
                requestHeaders.Add("Mimetype", "text/*");
                httpRequest.ContentType = @"application/x-www-form-urlencoded";
                foreach (var header in requestHeaders)
                {
                    httpRequest.Headers.Set(header.Key, header.Value);
                }

                httpRequest.Method = "POST";
                body = "jsondata=" + queryBody;
                if (string.IsNullOrEmpty(body))
                {
                    httpRequest.ContentLength = 0;
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    httpRequest.ContentLength = bytes.Length;
                    // Get the request stream and write the post data in.
                    Stream requestStream = httpRequest.GetRequestStream();
                    requestStream.Write(bytes, 0, bytes.Length);
                }
                var response = httpRequest.GetResponse();
                using (var datastream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(datastream))
                    {
                        responseContent = reader.ReadToEnd();
                    }
                }
                return responseContent;
            }
            catch (Exception e)
            {
                //write Log
                throw e;
            }
        }

        #endregion

        public FileResponseEntity GetFilePreSignedUrl(FileRequestEntity reqEntity, int urlValidityInMinutes)
        {
            string Url = string.Empty;
            FileResponseEntity fileResponse = null;
            string keyName = reqEntity.Location + Slash.FrontSlash.GetDescription() + reqEntity.OriginalName;

            if (string.IsNullOrEmpty(reqEntity.Location) || string.IsNullOrEmpty(reqEntity.OriginalName))
            {
                throw new ArgumentException("Wrong params sent to GetFile(FileRequestEntity chunkedReqEntity, long offset, long length): " + reqEntity.Location + ", " + reqEntity.OriginalName);
            }
            if (urlValidityInMinutes <= 0)
            {
                throw new ArgumentException("Invalid value [\"{0}\"] provided for urlValidityInMinutes!");
            }

            try
            {
                //ResponseHeaderOverrides res = new ResponseHeaderOverrides().
                // reqEntity.httpContext.Response.AddHeader(reqEntity.SourceFileName);
                GetPreSignedUrlRequest preSignedReq = new GetPreSignedUrlRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Key = (keyName),
                    Protocol = (Protocol.HTTPS),
                    Expires = (DateTime.UtcNow.AddMinutes(urlValidityInMinutes)),
                    //.WithServerSideEncryptionMethod(ServerSideEncryptionMethod.AES256)
                    Verb = (HttpVerb.GET)
                };
                //.WithResponseHeaderOverrides(reqEntity.httpContext.Response.AddHeader(reqEntity.SourceFileName))


                Url = _s3Client.GetPreSignedURL(preSignedReq);
                if (Url != null)
                {
                    fileResponse = new FileResponseEntity();

                    fileResponse.OperationStatus = true;
                    fileResponse.PresignedURL = Url;
                }
                return fileResponse;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetFilePreSignedUrl Failed! keyName:{0}!", keyName);
                throw;
            }
        }

        #region Directory
        public FileResponseEntity CreateDirectory(string Location)
        {
            FileResponseEntity fileResentity = new FileResponseEntity();
            Location = Location + Slash.FrontSlash.GetDescription();
            PutObjectResponse response = null;
            try
            {
                PutObjectRequest request = new PutObjectRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Key = (Location)
                };

                request.InputStream = new MemoryStream();
                response = _s3Client.PutObject(request);
                fileResentity.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.CreateDirectory Failed! Location:{0}!", Location);
                throw;
            }
            return fileResentity;
        }
        public FileResponseEntity DeleteDirectory(string Location, bool recursive)
        {
            FileResponseEntity fileResentity = new FileResponseEntity();
            string preFixPath = Location + Slash.FrontSlash.GetDescription();
            try
            {
                ListObjectsRequest request = new ListObjectsRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Prefix = (preFixPath)
                };
                ListObjectsResponse response = _s3Client.ListObjects(request);

                foreach (S3Object entry in response.S3Objects)
                {
                    this.DeleteFile(string.Empty, entry.Key, true);
                }
                ///If it run the loop it should be successful, If folder is not there still it will say folder deleted by returning true 
                fileResentity.OperationStatus = true;


            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.DeleteDirectory Failed! Location:{0}!", Location);
                fileResentity.OperationStatus = false;
                throw ex;
            }
            return fileResentity;
        }
        #endregion Directory

        public FileResponseEntity DeleteFile(string Location, string DocumentName, bool Recursive)
        {
            FileResponseEntity fre = new FileResponseEntity();
            string key = (!string.IsNullOrEmpty(Location) ? (Location + Slash.FrontSlash.GetDescription()) : string.Empty) + DocumentName;
            DeleteObjectResponse response = null;
            try
            {
                DeleteObjectRequest request = new DeleteObjectRequest()
                    {
                        BucketName = AccountNeutralS3BucketName,
                        Key = key
                    };
                response = _s3Client.DeleteObject(request);
                fre.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.DeleteFile Failed! Location:{0}, DocumentName:{1}, Recursive:{2}!", Location, DocumentName, Recursive);
                throw;
            }
            return fre;
        }

        public FileResponseEntity CopyFile(string sourceDir, string destinationDir, string oldFileName, string newFileName)
        {
            FileResponseEntity fileResentity = new FileResponseEntity();
            CopyObjectRequest copyRequest = null;
            CopyObjectResponse copyResponse = null;
            try
            {
                copyRequest = new CopyObjectRequest()
                {
                    DestinationBucket = AccountNeutralS3BucketName,
                    DestinationKey = destinationDir + Slash.FrontSlash.GetDescription() + newFileName,
                    SourceBucket = AccountNeutralS3BucketName,
                    SourceKey = sourceDir + Slash.FrontSlash.GetDescription() + oldFileName,
                    Timeout = new TimeSpan(0, 0, 0, 0, FileSystemTimeOut),
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };
                copyResponse = _s3Client.CopyObject(copyRequest);
                fileResentity.OperationStatus = true;
            }
            catch (AmazonS3Exception aEX)
            {
                try
                {
                    FsLogManager.Fatal(aEX, "AwsS3FileSystem.CopyFile Failed! Source Dir:{0}, destination Dir:{1}, Old FileName:{2}, New FileName:{3}!", sourceDir, destinationDir, oldFileName, newFileName);
                    Thread.Sleep(base.WaitDurationBeforeRetryUploadOrDownloadInMs);
                    ///if file is present the file will return some value
                    long length = GetFileContentLength(destinationDir, newFileName);
                    copyResponse = _s3Client.CopyObject(copyRequest);
                }
                catch (AmazonS3Exception x)
                {
                    if (x.ErrorCode == "NoSuchKey")
                    {
                        FsLogManager.Fatal(x, "AwsS3FileSystem.CopyFile Failed! Source Dir:{0}, destination Dir:{1}, Old FileName:{2}, New FileName:{3}!", sourceDir, destinationDir, oldFileName, newFileName);
                    }
                }
                catch (Exception x)
                {
                    FsLogManager.Fatal(x, "AwsS3FileSystem.CopyFile Failed! Source Dir:{0}, destination Dir:{1}, Old FileName:{2}, New FileName:{3}!", sourceDir, destinationDir, oldFileName, newFileName);
                    throw;
                }

            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.CopyFile Failed! Location:{0}\\{1}!", sourceDir, oldFileName);
                throw;
            }
            return fileResentity;
        }

        public ListObjectsResponse GetDirectoryListing(string Location, string directoryName)
        {
            ListObjectsResponse res = null;
            try
            {
                ListObjectsRequest req = new ListObjectsRequest();
                PutObjectRequest request = new PutObjectRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Key = (Location)
                };
                req = new ListObjectsRequest()
                {
                    BucketName = (AccountNeutralS3BucketName),
                    Prefix = (directoryName)
                };
                res = _s3Client.ListObjects(req);
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "AwsS3FileSystem.GetDirectoryListing Failed! Location:{0}, directoryName{1}!", Location, directoryName);
                throw;
            }
            return res;
        }

        #region Not Implemented Methods

        public FileResponseEntity PutFile(ref MemoryStream Image, string Location, string originalName, bool overwrite)
        {
            throw new NotImplementedException();
        }
        public string GetDataNodeFromNameNode(string Location, string DocumentName, string MethodType, bool needMasterNodeOnly)
        {
            throw new NotImplementedException();
        }
        #endregion Not Implemented Methods
    }
}


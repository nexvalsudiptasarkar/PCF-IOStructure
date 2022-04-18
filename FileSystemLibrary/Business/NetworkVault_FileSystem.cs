using Amazon.S3.Model;
using FileSystemLib.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Web;


namespace FileSystemLib
{
    internal class NetworkVaultFileSystem : FileSystemBase, IFileSystemLib
    {
        #region Constructor
        public NetworkVaultFileSystem(ISharedAppSettings sharedAppSettings)
            : base(sharedAppSettings)
        {
        }
        #endregion

        public FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite, string s3Bucket)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(string DocumentName, string accountId, string containerId)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETag, string accountId)
        {
            throw new NotImplementedException();
        }

        private string GetPresignedURLForChunks(string fileName, string accountId, string projectId)
        {
            throw new NotImplementedException();
        }

        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, string accountId)
        {
            throw new NotImplementedException();
        }

        public string generateUploadID(string FolderName, string keyName, string accountId)
        {
            throw new NotImplementedException();
        }

        public bool GetFileAndSave(string saveLocation, string DocumentName, string accountId, string containerId)
        {
            throw new NotImplementedException(); 
        }

        public FileResponseEntity GetFile(string DocumentName, string accountId, string containerId, HttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        // Changes Made By Sudipta PutFile New Implementation for java using Put Request for less than 5 mb chunk 04-03-2013
        #region GetFile
        public Stream GetFile(string Location, string DocumentName, ref WebRequest request, ref WebResponse response, IDictionary<string, object> _dictionary)
        {
            Stream stream = null;
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(Location + Slash.FrontSlash.GetDescription() + DocumentName);
                stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                FsLogManager.Info("Stream for File:{0} opened successfully!", fileInfo.FullName);
                return stream;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to get file stream for file:{0}!", fileInfo.FullName);
                return null;
            }
        }

        public FileResponseEntity GetFile(FileRequestEntity chunkedReqEntity, long offset, long length)
        {
            FileResponseEntity fileResponse = null;
            HttpContext httpContx = chunkedReqEntity.httpContext;
            string requestingURL = chunkedReqEntity.Location;
            string DocumentName = chunkedReqEntity.OriginalName;
            FileInfo fileInfo = null;
            try
            {
                int readchunklength = default(int);
                long chunkSize = DownloadBufferSize;
                byte[] buffer = new byte[chunkSize];

                fileInfo = new FileInfo(requestingURL + Slash.FrontSlash.GetDescription() + DocumentName);
                Stream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                // httpContx.Response.AddHeader("Content-Length", fileInfo.Length.ToString());
                if (stream != null && stream.CanSeek)
                    stream.Seek(offset, SeekOrigin.Begin);
                while (length > 0 && httpContx.Response.IsClientConnected)
                {
                    readchunklength = stream.Read(buffer, 0, length > chunkSize ? (int)chunkSize : (int)length);
                    httpContx.Response.OutputStream.Write(buffer, 0, readchunklength);
                    length = length - readchunklength;
                    httpContx.Response.Flush();
                }
                if (httpContx.Response.OutputStream.CanWrite)
                {
                    fileResponse = new FileResponseEntity();
                    //fileResponse.CompletedByte = bytesProcessed;
                    fileResponse.ContentLength = fileInfo.Length;
                    //context.Response.AddHeader("Content-Length", bytesProcessed.ToString());
                    //context.Response.Flush();/// Auto view markup will not work if we do flushing here.
                    fileResponse.OperationStatus = true;
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to read file stream for chunked-Request-Entity, input file:{0}, Requested Offset:{1}, Requested Length:{2}!", fileInfo.FullName, offset, length);
                throw;
            }
            return fileResponse;
        }

        public FileResponseEntity GetFile(FileRequestEntity chunkedReqEntity)
        {
            FileResponseEntity fileResponse = null;
            HttpContext httpContx = chunkedReqEntity.httpContext;
            string requestingURL = chunkedReqEntity.Location;
            string DocumentName = chunkedReqEntity.OriginalName;
            FileInfo fileInfo = null;

            try
            {
                long chunkSize = DownloadBufferSize;
                byte[] buffer = new byte[chunkSize];
                fileInfo = new FileInfo(requestingURL + Slash.FrontSlash.GetDescription() + DocumentName);
                Stream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (stream != null)
                    httpContx.Response.AppendHeader("Content-Length", stream.Length.ToString());

                int dataToRead;
                while ((dataToRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    httpContx.Response.OutputStream.Write(buffer, 0, dataToRead);
                    httpContx.Response.Flush();
                }

                if (httpContx.Response.OutputStream.CanWrite)
                {
                    fileResponse = new FileResponseEntity();
                    //fileResponse.StatusCode = httpContx.Response.StatusCode;
                    //fileResponse.ContentLength = httpContx.Response.OutputStream.Length;
                    //context.Response.AddHeader("Content-Length", bytesProcessed.ToString());
                    //context.Response.Flush();/// Auto view markup will not work if we do flushing here.
                    fileResponse.OperationStatus = true;
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to read file stream for chunked-Request-Entity, input file:{0}!", fileInfo.FullName);
                throw;
            }
            return fileResponse;
        }

        public long GetFileContentLength(string fileLocation, string fileName)
        {

            try
            {
                FileInfo objFileInfo = new FileInfo(fileLocation + Slash.FrontSlash.GetDescription() + fileName);
                if (objFileInfo.Exists)
                {
                    return objFileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to get file content length, input file:{0}\\{1}!", fileName, fileLocation);
                throw;
            }
            return default(long);

        }

        #endregion GetFile
        #region PutFile()
        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length)
        {
            Stream ipstream = fileReqEntity.Stream;
            string DocumentPath = fileReqEntity.Location;
            string DocumentName = fileReqEntity.OriginalName;
            string uploadFilePathNName = string.Format("{0}\\{1}", DocumentPath, DocumentName);
            FileResponseEntity fileResponse = new FileResponseEntity();
            byte[] Buffer = new byte[UploadBufferSize];
            try
            {
                CreateDirectory(DocumentPath);
                int dataToRead;
                using (FileStream writerStr = File.Create(uploadFilePathNName))
                {
                    //ipstream.ReadTimeout = -1;
                    while ((dataToRead = ipstream.Read(Buffer, default(int), Buffer.Length)) > default(int))
                    {
                        writerStr.Write(Buffer, 0, dataToRead);
                        writerStr.Flush();
                        fileResponse.OperationStatus = true;
                    }
                }
                ipstream.Close();
            }
            catch (Exception ex)
            {
                fileResponse.OperationStatus = false;
                FsLogManager.Fatal(ex, "Failed to PUT file, destination file:{0}!", uploadFilePathNName);
                throw;
            }
            return fileResponse;
        }
        #endregion PutFile()

        #region DeleteFile
        public FileResponseEntity DeleteFile(string Location, string DocumentName, bool Recursive)
        {
            FileResponseEntity fileResEntity = new FileResponseEntity();
            FileInfo fs = null;
            DateTime dt = DateTime.Now;
            try
            {
                fs = new FileInfo(Location + Slash.FrontSlash.GetDescription() + DocumentName);
                if (fs.Exists)
                {
                    if (fs.LastAccessTime < dt.Subtract(TimeSpan.FromMinutes(MaxDurationInMinuteAfterLastFileAccessForDelete)))
                    {
                        fs.Delete();
                        fileResEntity.OperationStatus = true;
                    }
                    else
                    {
                        FsLogManager.Info("NetworkVaultFileSystem.DeleteFile(): File Name:{0}, Last Access Time:{1}, Last File Access Time In Minute For Delete:{2}", fs.FullName, fs.LastAccessTime, MaxDurationInMinuteAfterLastFileAccessForDelete);
                    }
                }
                else
                {
                    FsLogManager.Fatal("File:{0}\\{1} doesn't exist!", Location, DocumentName);
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to delete file:{0}\\{1}!", Location, DocumentName);
                throw;
            }

            return fileResEntity;
        }
        #endregion DeleteFile

        #region Directory
        public FileResponseEntity CreateDirectory(string Location)
        {
            FileResponseEntity objFileResponseEntity = new FileResponseEntity();
            if (!Directory.Exists(Location))
            {
                Directory.CreateDirectory(Location);
                objFileResponseEntity.OperationStatus = true;
            }
            return objFileResponseEntity;
        }
        public FileResponseEntity DeleteDirectory(string Location, bool recursive)
        {
            FileResponseEntity fileResEntity = new FileResponseEntity();
            DateTime dt = DateTime.Now;
            DirectoryInfo dirInfo = null;
            try
            {
                dirInfo = new DirectoryInfo(Location);
                if (dirInfo.Exists)
                {
                    ///directory info LastAccessTime is reliable, because it gives the last update time of nested subfolder or file in resides inside subfolder
                    if (dirInfo.LastAccessTime < dt.Subtract(TimeSpan.FromMinutes(MaxDurationInMinuteAfterLastFileAccessForDelete)))
                    {
                        dirInfo.Delete(recursive);
                        fileResEntity.OperationStatus = true;
                    }
                    else
                    {
                        FsLogManager.Warn("NetworkVaultFileSystem.DeleteDirectory() Delete directory not attempted for '{0}'!", dirInfo.FullName);
                    }
                }
                else
                {
                    FsLogManager.Warn("NetworkVaultFileSystem.DeleteDirectory() Directory '{0}' not Found!", dirInfo.FullName);
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "NetworkVaultFileSystem: Failed Delete Directory:{0}!", dirInfo.FullName);
                throw;
            }

            return fileResEntity;
        }
        #endregion Directory

        #region CopyFile
        public FileResponseEntity CopyFile(string sourceDir, string destinationDir, string oldFileName, string newFileName)
        {
            FileResponseEntity objFileResponseEntity = new FileResponseEntity();
            try
            {
                CreateDirectory(destinationDir);
                string sourceFile = sourceDir + Slash.FrontSlash.GetDescription() + oldFileName;
                string destinationFile = destinationDir + Slash.FrontSlash.GetDescription() + newFileName;
                File.Copy(sourceFile, destinationFile);
                objFileResponseEntity.OperationStatus = true;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "NetworkVaultFileSystem.CopyFile: Copy File Failed! Location:{0}\\{1}!", sourceDir, oldFileName);
                throw;
            }
            return objFileResponseEntity;
        }
        #endregion CopyFile

        #region Not Implimented Methods
        public FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity PutFile(ref MemoryStream Image, string Location, string originalName, bool overwrite)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, bool DirectUpload)
        {
            throw new NotImplementedException();
        }
        public string generateUploadID(string FolderName, string keyName)
        {
            throw new NotImplementedException();
        }

        public string GetDataNodeFromNameNode(string Location, string DocumentName, string MethodType, bool needMasterNodeOnly)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETags)
        {
            throw new NotImplementedException();
        }
        public int FolderWiseItemInfo(string folderNamelocation)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity GetFilePreSignedUrl(FileRequestEntity reqEntity, int urlValidityInMinutes)
        {
            throw new NotImplementedException();
        }

        public string GetObjectDetails(string keyName)
        {
            throw new NotImplementedException();
        }
        public ListObjectsResponse GetDirectoryListing(string Location, string DIR_NAME)
        {
            throw new NotImplementedException();
        }
        #endregion Not Implimented Methods

        //Atanu Banik 12-Jun-2015: The abstraction is already broken, NEED TO REDESIGN THIS CLASS LIBRARY! Adding 1 more method which will make this worse!
        public bool SaveFile(string sourceLocation, string sourceDocumentName, string destinationFile)
        {
            throw new NotImplementedException();
        }
    }
}

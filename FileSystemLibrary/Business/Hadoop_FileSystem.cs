using Amazon.S3.Model;
using FileSystemLib.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace FileSystemLib
{
    internal sealed class HadoopFileSystem : FileSystemBase, IFileSystemLib
    {
        #region Constructor
        public HadoopFileSystem(KeyValuePair<string, string>? awsAccessKeys)
            : base(awsAccessKeys)
        {
        }

        #endregion

        public FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite, string s3Bucket)
        {
            throw new NotImplementedException();
        }

        public FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETag, string s3Bucket)
        {
            throw new NotImplementedException();
        }

        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, string s3Bucket)
        {
            throw new NotImplementedException();
        }

        public string generateUploadID(string FolderName, string keyName, string s3Bucket)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(string DocumentName, string accountId, string containerId)
        {
            throw new NotImplementedException();
        }

        private string GetPresignedURLForChunks(string fileName, string accountId, string projectId)
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
        # region PutFile()
        public FileResponseEntity PutFile(ref MemoryStream Stream, string Location, string originalName, bool overwrite)
        {
            FileResponseEntity fre = null;
            int streamLength = (int)Stream.Length;
            if (streamLength == default(int) || streamLength > 10 * 1024)
            {
                fre = new FileResponseEntity();
                fre.CompletedByte = streamLength;
                //fileResEntity.errorMessage = "File size is bigger than 10KB";
                fre.OperationStatus = false;
                FsLogManager.Fatal("HadoopFileSystem.PutFile failed as stream length = {0}!", streamLength);
                return fre;
            }
            string uploadUrl = GetUploadUrl(Location, originalName, overwrite);
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream reqStream = null;
            try
            {
                string dataNodeLocation = GetDataNodeFromNameNode(uploadUrl, MethodType.PUT.ToString());
                request = (HttpWebRequest)WebRequest.Create(dataNodeLocation);
                request.Method = MethodType.PUT.ToString(); // you might use "POST"
                request.ContentLength = streamLength;
                if (request.GetRequestStream() != null)
                    reqStream = request.GetRequestStream();
                reqStream.Write(Stream.ToArray(), 0, streamLength);
                reqStream.Flush();
                response = request.GetResponse();
                if (response != null)
                {
                    fre = new FileResponseEntity();
                    fre.CompletedByte = streamLength;
                    fre.OperationStatus = true;
                }
                FsLogManager.Fatal("HadoopFileSystem.PutFile successful, {0} bytes written successfully!", streamLength);
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "HadoopFileSystem.PutFile Failed, upload URL:{0}, Stream Length:{1}!", uploadUrl, streamLength);
                throw;
            }
            finally
            {
                if (reqStream != null)
                {
                    reqStream.Close(); reqStream.Dispose();
                }
                if (request != null)
                {
                    request.Abort();
                }
                if (Stream != null)
                {
                    Stream.Flush();
                    Stream.Close();
                    Stream.Dispose();
                }
            }
            return fre;
        }

        public FileResponseEntity PutFile(FileRequestEntity fre, long offset, long length)
        {
            string sourceFileName = fre.SourceFileName;
            string Location = fre.Location;
            string originalName = fre.OriginalName;
            bool overwrite = fre.Overwrite;

            FileResponseEntity fileResEntity = null;
            string uploadUrl = GetUploadUrl(Location, originalName, overwrite);
            string sourceTempFile = AppServerTempDir + sourceFileName;

            using (FileDeleteHelper _disposer = new FileDeleteHelper(sourceTempFile))
            {
                try
                {
                    string dataNodeLocation = GetDataNodeFromNameNode(uploadUrl, MethodType.PUT.ToString());
                    if (base.IsUsingWebClient)
                        WebClientPutFile(sourceTempFile, dataNodeLocation, ref fileResEntity);
                    else
                        WebRequestPutFile(sourceTempFile, dataNodeLocation, ref fileResEntity);
                }
                catch (Exception ex)
                {
                    FsLogManager.Fatal(ex, "HadoopFileSystem.PutFile Failed, upload URL:{0}, Source Temp File:{1}, Offset:{2}, Length:{3}!", uploadUrl, sourceTempFile, offset, length);
                    throw;
                }
            }
            return fileResEntity;

        }
        public FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, bool DirectUpload)
        {
            throw new NotImplementedException();
        }
        private string GetUploadUrl(string Location, string originalName, bool overwrite)
        {
            StringBuilder uploadUrl = new StringBuilder();
            uploadUrl.Append(HadoopURL);
            uploadUrl.Append(BaseFolder);
            uploadUrl.Append(Location);
            uploadUrl.Append(EnumHelper.GetDescription(Slash.FrontSlash));
            uploadUrl.Append(originalName);
            uploadUrl.Append(EnumHelper.GetDescription(Operation.CREATE));
            uploadUrl.Append(EnumHelper.GetDescription(Parameters.bufferSize));
            uploadUrl.Append(UploadBufferSize);
            if (overwrite)
            {
                uploadUrl.Append(EnumHelper.GetDescription(Parameters.overwrite));
                uploadUrl.Append(overwrite);
            }

            return uploadUrl.ToString();
        }
        private void WebRequestPutFile(string sourceTempFile, string uploadUrl, ref FileResponseEntity fileResEntity)
        {
            DateTime starttime = DateTime.Now;
            long bytesProcessed = default(long);
            FileStream fileStream = null;
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream reqStream = null;
            byte[] buffer = new byte[RestUploadBufferSize];
            int bytesRead = default(int);
            try
            {
                FileInfo fileInfo = new FileInfo(sourceTempFile);
                fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                request = (HttpWebRequest)WebRequest.Create(uploadUrl);
                request.Method = MethodType.PUT.ToString(); // you might use "POST"
                request.ContentLength = fileStream.Length;
                //request.AllowWriteStreamBuffering = true;
                request.Timeout = FileSystemBase.FileSystemTimeOut;
                if (request.GetRequestStream() != null)
                    reqStream = request.GetRequestStream();

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    //DateTime startTime = DateTime.Now;
                    ////bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    //LogManager.Trace("------Loop Chunk read time", bytesRead + ":" + DateTime.Now.Subtract(startTime).ToString());
                    DateTime startTime1 = DateTime.Now;
                    reqStream.Write(buffer, 0, bytesRead);
                    //reqStream.BeginWrite(,
                    FsLogManager.Info("HadoopFileSystem.WebRequestPutFile Loop Chunk write... upload URL:{0}, Source Temp File:{1}, Total bytes processed:{2}!!", uploadUrl, sourceTempFile, bytesRead);
                    bytesProcessed += bytesRead;
                    reqStream.Flush();
                }

                response = request.GetResponse();
                if (response != null)
                {
                    fileResEntity = new FileResponseEntity();
                    fileResEntity.CompletedByte = bytesProcessed;
                    fileResEntity.OperationStatus = true;
                    //fileResponse.ContentLength = response.ContentLength;
                    //fileResponse.statusCode = ((HttpWebResponse)(response)).StatusCode;
                }
                FsLogManager.Info("HadoopFileSystem.WebRequestPutFile successful, upload URL:{0}, Source Temp File:{1}, Total bytes processed:{2}!!", uploadUrl, sourceTempFile, bytesProcessed);
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "HadoopFileSystem.WebRequestPutFile Failed, upload URL:{0}, Source Temp File:{1}!!", uploadUrl, sourceTempFile);
                throw;
            }
            finally
            {
                if (fileStream != null) { fileStream.Close(); fileStream.Dispose(); }
                if (reqStream != null) { reqStream.Close(); reqStream.Dispose(); }
                if (request != null) request.Abort();
            }
        }


        private void WebClientPutFile(string sourceTempFile, string uploadUrl, ref FileResponseEntity fileResEntity)
        {
            DateTime starttime = DateTime.Now;
            ARCWebClient wcClent = new ARCWebClient();
            byte[] bytesRead = null;

            try
            {
                wcClent.GetLifetimeService();
                bytesRead = wcClent.UploadFile(uploadUrl, MethodType.PUT.ToString(), sourceTempFile);
                if (bytesRead.Length > 0)
                {
                    fileResEntity = new FileResponseEntity();
                    fileResEntity.CompletedByte = bytesRead.Length;
                    fileResEntity.OperationStatus = true;
                }
                FsLogManager.Info("FileSystem.WebClientPutFile() successful. Upload URL:{0}, sourceTempFile:{1}, Total bytes uploaded:{2}", uploadUrl, sourceTempFile, bytesRead.Length);
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "FileSystem.WebClientPutFile() Failed. Upload URL:{0}, sourceTempFile:{1}, Total bytes uploaded:{2}", uploadUrl, sourceTempFile, bytesRead.Length);
            }
            finally
            {
                if (wcClent != null)
                {
                    wcClent.Dispose();
                }
            }
        }


        # endregion PutFile()
        # region GetFile()
        public FileResponseEntity GetFile(FileRequestEntity fileReqEntity)
        {
            HttpContext context = fileReqEntity.httpContext;
            string Location = fileReqEntity.Location;
            string DocumentName = fileReqEntity.OriginalName;

            string downloadUrl = GetDownloadUrl(Location, DocumentName, default(long), default(long));
            return GetFile(context, Location, DocumentName, downloadUrl);
        }
        public FileResponseEntity GetFile(FileRequestEntity chunkedReqEntity, long offset, long length)
        {
            HttpContext context = chunkedReqEntity.httpContext;
            string Location = chunkedReqEntity.Location;
            string DocumentName = chunkedReqEntity.OriginalName;
            string downloadUrl = GetDownloadUrl(Location, DocumentName, offset, length);
            return GetFile(context, Location, DocumentName, downloadUrl.ToString());
        }

        private string GetDownloadUrl(string Location, string DocumentName, long offset, long length)
        {
            StringBuilder downloadUrl = new StringBuilder();
            downloadUrl.Append(HadoopURL);
            downloadUrl.Append(BaseFolder);
            downloadUrl.Append(Location);
            downloadUrl.Append(EnumHelper.GetDescription(Slash.FrontSlash));
            downloadUrl.Append(DocumentName);
            downloadUrl.Append(EnumHelper.GetDescription(Operation.OPEN));
            downloadUrl.Append(EnumHelper.GetDescription(Parameters.bufferSize));
            downloadUrl.Append(RestDownloadBufferSize);
            if (offset > default(long))
            {
                downloadUrl.Append(EnumHelper.GetDescription(Parameters.offset));
                downloadUrl.Append(offset);
            }
            if (length > default(long))
            {
                downloadUrl.Append(EnumHelper.GetDescription(Parameters.length));
                downloadUrl.Append(length);
            }
            return downloadUrl.ToString();
        }
        private FileResponseEntity GetFile(HttpContext context, string Location, string DocumentName, string downloadUrl)
        {
            FileResponseEntity fileResponse = null;
            byte[] buffer = new byte[DownloadBufferSize];
            long bytesProcessed = 0;
            Stream stream = null;
            WebRequest request = null;
            WebResponse response = null;
            try
            {
                request = WebRequest.Create(GetDataNodeFromNameNode(downloadUrl, MethodType.GET.ToString()));
                if (request != null)
                {
                    response = request.GetResponse();
                    if (response != null)
                    {
                        stream = response.GetResponseStream();
                    }
                }
                int bytesRead;
                //context.Response.AddHeader("Content-Length", response.ContentLength.ToString());
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
                    context.Response.AddHeader("Content-Length", bytesProcessed.ToString());
                    //context.Response.Flush();/// Auto view markup will not work if we do flushing here.
                    fileResponse.OperationStatus = true;
                }
                FsLogManager.Info("FileSystem.GetFile() successful. download URL:{0}, bytes Processed:{1}!", downloadUrl, bytesProcessed);
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "FileSystem.GetFile() Failed. download URL:{0}, bytes Processed:{1}!", downloadUrl, bytesProcessed);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            return fileResponse;
        }
        public Stream GetFile(string Location, string DocumentName, ref WebRequest request, ref WebResponse response, IDictionary<string, object> _dictionary)
        {
            byte[] buffer = new byte[DownloadBufferSize];
            Stream stream = null;
            string downloadUrl = GetDownloadUrl(Location, DocumentName, default(long), default(long));

            try
            {
                request = WebRequest.Create(GetDataNodeFromNameNode(downloadUrl, MethodType.GET.ToString()));
                if (request != null)
                {
                    response = request.GetResponse();
                    if (response != null)
                    {
                        stream = response.GetResponseStream();
                    }
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "FileSystem.GetFile(): Failed open stream from download URL:{0}!", downloadUrl);
                throw;
            }
            return stream;
        }

        # endregion GetFile()
        #region CopyFile()

        public FileResponseEntity CopyFile(string sourceDir, string destinationDir, string oldFileName, string newFileName)
        {
            DateTime starttime = DateTime.Now;
            FileResponseEntity fileResEntity = null;
            HttpWebRequest request = null;
            WebResponse response = null;
            StringBuilder sourcePath = new StringBuilder();
            sourcePath.Append(HadoopCopyURL);
            sourcePath.Append(BaseFolder);
            sourcePath.Append(sourceDir);
            sourcePath.Append(EnumHelper.GetDescription(Slash.FrontSlash));
            sourcePath.Append(oldFileName);
            StringBuilder detinationPath = new StringBuilder();
            detinationPath.Append(HadoopCopyURL);
            detinationPath.Append(BaseFolder);
            detinationPath.Append(destinationDir);
            detinationPath.Append(EnumHelper.GetDescription(Slash.FrontSlash));
            detinationPath.Append(newFileName);
            try
            {
                request = (HttpWebRequest)WebRequest.Create(sourcePath.ToString());
                request.Method = MethodType.COPY.ToString();
                request.Headers.Add("Depth", "infinity");
                request.Headers.Add("Destination", detinationPath.ToString());
                //request.Headers.Add("Overwrite", "T");
                request.AllowAutoRedirect = false;
                request.Timeout = FileSystemTimeOut;
                response = request.GetResponse();

                if (response != null)
                {
                    fileResEntity = new FileResponseEntity();
                    fileResEntity.OperationStatus = true;
                }
            }
            //catch (WebException webEx) {} //todo temp sol for 403 forbidden             
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "FileSystem: Failed Copy File from:{0}\\{1} to:{2}\\{3}!", sourceDir, oldFileName, destinationDir, newFileName);
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            return fileResEntity;
        }

        #endregion CopyFile()
        #region Directory()
        public FileResponseEntity CreateDirectory(string Location)
        {
            FileResponseEntity fileResponse = null;
            StringBuilder uploadUrl = new StringBuilder();
            uploadUrl.Append(HadoopURL);
            uploadUrl.Append(BaseFolder);
            uploadUrl.Append(Location);
            uploadUrl.Append(EnumHelper.GetDescription(Operation.MKDIRS));

            HttpWebRequest request = null;
            WebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(uploadUrl.ToString());
                request.Method = MethodType.PUT.ToString(); // you might use "POST"
                request.ContentLength = default(long);
                request.AllowWriteStreamBuffering = true;
                ReadJSONResponse(ref fileResponse, ref request, ref response);
                FsLogManager.Info("FileSystem: Create Directory Successful, upload URL:{0}, HTTP Status Code:{1}", Location, ((HttpWebResponse)(response)).StatusCode);
            }

            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "FileSystem: Failed Create Directory, Location:{0}!", Location);
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            return fileResponse;
        }
        public FileResponseEntity DeleteDirectory(string Location, bool recursive)
        {
            throw new NotImplementedException();
        }
        #endregion Directory()
        #region DeleteFile()

        public FileResponseEntity DeleteFile(string Location, string DocumentName, bool Recursive)
        {
            FileResponseEntity fileResponseEntity = null;
            HttpWebRequest request = null;
            WebResponse response = null;
            string deleteUrl = GetDeleteUrl(Location, DocumentName, Recursive);
            try
            {
                request = (HttpWebRequest)WebRequest.Create(deleteUrl);
                request.Method = MethodType.DELETE.ToString();
                request.AllowAutoRedirect = false;

                ReadJSONResponse(ref fileResponseEntity, ref request, ref response);
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to delete file:{0}!", deleteUrl);
                throw;
            }
            finally
            {
                if (request != null) request.Abort();
                if (response != null) response.Close();
            }
            return fileResponseEntity;

        }

        private string GetDeleteUrl(string Location, string DocumentName, bool Recursive)
        {
            StringBuilder uri = new StringBuilder();
            uri.Append(HadoopURL);
            uri.Append(BaseFolder);
            uri.Append(Location);
            uri.Append(EnumHelper.GetDescription(Slash.FrontSlash));
            uri.Append(DocumentName);
            uri.Append(EnumHelper.GetDescription(Operation.DELETE));
            uri.Append(EnumHelper.GetDescription(Parameters.recursive));
            uri.Append(Recursive);
            return uri.ToString();
        }
        #endregion DeleteFile()

        private static void ReadJSONResponse(ref FileResponseEntity fileResponseEntity, ref HttpWebRequest request, ref WebResponse response)
        {
            response = request.GetResponse();
            if (response != null)
            {
                fileResponseEntity = new FileResponseEntity();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    fileResponseEntity = js.Deserialize<FileResponseEntity>(reader.ReadToEnd());
                    fileResponseEntity.OperationStatus = fileResponseEntity.Boolean;
                }
            }
        }
        public string GetDataNodeFromNameNode(string Location, string DocumentName, string MethodType, bool needMasterNodeOnly)
        {
            string downloadUrl = GetDownloadUrl(Location, DocumentName, default(long), default(long));
            if (!needMasterNodeOnly)
                downloadUrl = GetDataNodeFromNameNode(downloadUrl, MethodType);
            return downloadUrl;
        }
        private string GetDataNodeFromNameNode(string Url, string MethodType)
        {
            HttpWebRequest MasterRequest = null;
            WebResponse MasterResponse = null;
            try
            {
                MasterRequest = (HttpWebRequest)WebRequest.Create(Url.ToString());
                MasterRequest.Method = MethodType;
                MasterRequest.AllowAutoRedirect = false;
                MasterResponse = MasterRequest.GetResponse();
                // Fetch data node location

                string dataNodeLocation = MasterResponse.Headers["Location"].ToString();//finding the SubNode 

                return dataNodeLocation;
            }
            finally
            {
                if (MasterResponse != null) MasterResponse.Close();
            }
        }

        #region Not Implimented Methods
        public long GetFileContentLength(string fileLocation, string fileName)
        {
            throw new NotImplementedException();
        }
        public string generateUploadID(string keyName)
        {
            throw new NotImplementedException();

        }
        public FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETags)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity PutFile(Stream ipstream, string keyName, string bucketName, long _startByte, long length, string UploadID, int PartNo)
        {
            throw new NotImplementedException();
        }

        public string generateUploadID(string FolderName, string keyName)
        {
            throw new NotImplementedException();
        }
        public int FolderWiseItemInfo(string folderName)
        {
            throw new NotImplementedException();
        }
        public FileResponseEntity GetFilePreSignedUrl(FileRequestEntity reqEntity)
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
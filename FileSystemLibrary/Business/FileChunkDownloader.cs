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
    internal sealed class FileChunkDownloader
    {
        #region Data Types
        #endregion

        #region Private Members
        private readonly string _fileChunkURLGeneratorService = null;
        private readonly string _fileChunkService = null;
        private readonly ISharedAppSettings _sharedAppSettings;
        private readonly IAmazonS3 _s3Client;
        #endregion

        #region Constructor
        public FileChunkDownloader(ISharedAppSettings sharedAppSettings, IAmazonS3 s3Client)
        {
            _sharedAppSettings = sharedAppSettings;
            _s3Client = s3Client;

            _fileChunkURLGeneratorService = string.Format("{0}/{1}", sharedAppSettings.FileChunkServiceEndpoint, sharedAppSettings.PreSignedURLGenerationCmd);
            _fileChunkService = string.Format("{0}/{1}", sharedAppSettings.FileChunkServiceEndpoint, sharedAppSettings.FileChunkServiceGetChunkInfoCmd);

            Trace.TraceInformation("FileChunkDownloader instantiated with FileChunkURLGeneratorService:{0} & FileChunkService:{1}, Hashcode:{2}.", _fileChunkURLGeneratorService, _fileChunkService, GetHashCode());
        }
        #endregion

        #region Public Methods
        public bool DownloadFile(string accountId, string projectId, string fileNameInS3WithoutPath, string bucketName, string destinationFileInLocalFs, bool trackProgress)
        {
            return StitchChunksToFile(accountId, projectId, fileNameInS3WithoutPath, bucketName, destinationFileInLocalFs);
        }
        #endregion

        #region Private Methods
        private ChunkMetaResponse[] getChunkInfo(string accountId, string projectId, int fileId, int revision)
        {
            string action = "GET_FILE_CHUNKS";
            StringBuilder queryBody = new StringBuilder();
            queryBody.Append('{')
                .Append('"').Append("action").Append('"').Append(':')
                .Append('"').Append(action).Append('"').Append(',')
                .Append("accountId").Append(':').Append(accountId).Append(',')
                .Append("projectId").Append(':').Append(projectId).Append(',')
                .Append("fileId").Append(':').Append(fileId).Append(',')
                .Append("revision").Append(':').Append(revision)
            .Append('}');

            string chunkInfoAsString = executeHbaseServiceApi(_fileChunkService, queryBody.ToString());

            if (string.IsNullOrEmpty(chunkInfoAsString))
                return null;

            {
                GeneralStringResponseList response = Newtonsoft.Json.JsonConvert.DeserializeObject<GeneralStringResponseList>(chunkInfoAsString);
                Newtonsoft.Json.Linq.JArray q = Newtonsoft.Json.Linq.JArray.Parse(response.values.First().chunksJson);
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(q);
                ChunkMetaResponse[] chunkOffsets = Newtonsoft.Json.JsonConvert.DeserializeObject<ChunkMetaResponse[]>(jsonString);

                if (chunkOffsets != null && chunkOffsets.Length > 0)
                { 
                    foreach(ChunkMetaResponse chunkOffset in chunkOffsets)
                        chunkOffset.rootFolder = response.values.First().rootFolder;
                    return chunkOffsets;
                }
            }
            return null;
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

        private bool extractFileAndRevisionId(string s3Bucket, string folderInS3Bucket, string fileNameInS3WithoutPath, ref int fileId, ref int revisionId)
        {
            char separator = '_';
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameInS3WithoutPath);
            if (!fileNameWithoutExtension.Contains(separator))
            {
                FsLogManager.Fatal("Invalid S3-Document:{0} encountered [within s3Bucket:{1}; Folder:{2}]. The separator '_' is not present in file name!", fileNameInS3WithoutPath, s3Bucket, folderInS3Bucket);
                return false;
            }

            string[] IDs = fileNameWithoutExtension.Split(separator);
            if (IDs.Length < 2)
            {
                FsLogManager.Fatal("Invalid S3-Document:{0} encountered [within s3Bucket:{1}; Folder:{2}]!", fileNameInS3WithoutPath, s3Bucket, folderInS3Bucket);
                return false;
            }
            int v = -1;
            foreach (var id in IDs)
            {
                if (!int.TryParse(id, out v))
                {
                    FsLogManager.Fatal("Invalid S3-Document:{0} encountered [within s3Bucket:{1}; Folder:{2}]!", fileNameInS3WithoutPath, s3Bucket, folderInS3Bucket);
                    return false;
                }
            }
            int.TryParse(IDs[0], out fileId);
            int.TryParse(IDs[1], out revisionId);
            return true;
        }

        /// <summary>
        /// will be called only for files uploaded through sync, hence s3 file name will like "FileId_RevisionId"
        /// </summary>
        /// <param name="Location">not used</param>
        /// <param name="fileNameInS3WithoutPath">s3 file name</param>
        /// <param name="s3Bucket">s3 bucket name</param>
        /// <param name="projectId">folder inside s3 bucket</param>
        /// <returns></returns>
        private bool StitchChunksToFile(string accountId, string projectId, string fileNameInS3WithoutPath,string bucketName, string destinationFileInLocalFs)
        {
            FsLogManager.Info(string.Format("Started stitching process for Document Name:{0}, accountid:{1}, FolderInS3Bucket:{2}...", fileNameInS3WithoutPath, accountId, projectId));
            List<string> downloadUris = new List<string>();

            try
            {
                int fileId = -1;
                int revisionId = -1;
                if (!extractFileAndRevisionId(accountId, projectId, fileNameInS3WithoutPath, ref  fileId, ref  revisionId))
                {
                    return false;
                }

                ChunkMetaResponse[] chunks = getChunkInfo(accountId, projectId, fileId, revisionId);
                if (chunks == null || chunks.Length <= 0)
                {
                    FsLogManager.Fatal("No Chunk-Info received for S3-Document:{0} [within accountid:{1}]!", fileNameInS3WithoutPath, accountId);
                    return false;
                }

                FsLogManager.Info(string.Format("Received {0}# of chunks, for S3-Document:{1}", chunks.Length, destinationFileInLocalFs));
                string url = string.Empty;
                using (S3Ops s3ops = new S3Ops(_sharedAppSettings))
                {
                    foreach (ChunkMetaResponse chunk in chunks)
                    {
                        url = s3ops.GeneratePreSignedDownloadUrl(bucketName, string.Format("{0}/{1}", chunk.rootFolder,chunk.chunkHeader + "_" + chunk.chunkHash), new TimeSpan(0, 15, 0));
                        downloadUris.Add(url);
                    }
                }
                int retryCount = 0;
                while (retryCount <= 3)
                {
                    List<string> downloadUrisFailedToDownload = null;
                    bool success = writeChunksToTemporaryFile(downloadUris, destinationFileInLocalFs, out downloadUrisFailedToDownload);
                    if (success)
                    {
                        return true;
                    }
                    downloadUris = downloadUrisFailedToDownload;
                    FsLogManager.Info(string.Format("Stiching failed mid-way! Trying again for {0} # of failed chunks, Retry Count:{1}.", downloadUris.Count, retryCount));
                    retryCount++;

                    if (retryCount > 3)
                    {
                        return false;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Failed to download chunks for S3-Document:{0}, for account:{1}! Exception:{2}", fileNameInS3WithoutPath, accountId, e.Message);
            }
            return false;
        }

        private string executeHbaseServiceApi(string requestUri, string queryBody)
        {
            if (string.IsNullOrEmpty(queryBody))
            {
                string s = string.Format("Invalid/Empty query string sent for querying service:{0}!", requestUri);
                throw new ArgumentException(s);
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(requestUri);

                var headers = new Dictionary<string, string>();
                headers.Add("Mimetype", "text/*");
                request.ContentType = @"application/x-www-form-urlencoded";

                foreach (var h in headers)
                {
                    request.Headers.Set(h.Key, h.Value);
                }

                request.Method = "POST";
                string body = "jsondata=" + queryBody;
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                request.ContentLength = bytes.Length;

                //Get the request stream and write the post data in.
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                var response = request.GetResponse();
                using (var s = response.GetResponseStream())
                {
                    using (var r = new StreamReader(s))
                    {
                        return r.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                FsLogManager.Warn(e, "HTTP call failed for URI:{0}; Body:{1}, Exception:{2}!", requestUri, queryBody, e.Message);
            }
            return null;
        }
        #endregion
    }
}


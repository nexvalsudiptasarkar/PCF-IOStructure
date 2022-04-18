using Amazon.S3.Model;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Web;

namespace FileSystemLib
{
    public interface IFileSystemLib
    {
        FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite, string s3Bucket);

        string generateUploadID(string FolderName, string keyName, string s3Bucket);

        FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, string s3Bucket);

        FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETag, string s3Bucket);

        Stream GetFile(string DocumentName, string accountId, string containerId);
        //set of overloaded functions which takes bucket-accountId as parameter

        FileResponseEntity GetFile(string DocumentName, string accountId, string containerId, HttpContext httpContext);

        bool GetFileAndSave(string saveLocation, string DocumentName, string accountId, string containerId);

        /// <summary>
        /// Downloads a file from S3 to Local File System
        /// </summary>
        /// <param name="awsSourceLocation"></param>
        /// <param name="awsSourceDocumentName"></param>
        /// <param name="localDestinationFile"></param>
        /// <remarks>Atanu Banik 12-Jun-2015: The abstraction is already broken, NEED TO REDESIGN THIS CLASS LIBRARY! Adding 1 more method which will make this worse!</remarks>
        bool SaveFile(string awsSourceLocation, string awsSourceDocumentName, string localDestinationFile);

        // Changes Made By Sudipta PutFile New Implementation for java using Put Request for less than 5 mb chunk 04-03-2013
        /// <summary>
        /// putting files to AWS or Hadoop as per required parameter
        /// </summary>
        /// <param name="fileReqEntity"></param>
        /// <returns></returns>
        FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length);
        /// <summary>
        /// Use for Small file taking from MemoryStream object, file size lesser than 10 KB
        /// </summary>
        /// <param name="Image"></param>
        /// <param name="projectLocation"></param>
        /// <param name="originalName"></param>
        /// <returns></returns>
        FileResponseEntity PutFile(ref MemoryStream Image, string Location, string originalName, bool overwrite);
        FileResponseEntity PutFile(string sourceFileLocation, string destinationLocation, string originalName, bool overwrite);
        FileResponseEntity PutFile(FileRequestEntity fileReqEntity, long Offset, long Length, bool DirectUpload);
        int FolderWiseItemInfo(string folderName);
        string generateUploadID(string FolderName, string keyName);
        /// <summary>
        /// Get the whole file in http context without seeking 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="Location"></param>
        /// <param name="DocumentName"></param>
        /// <returns></returns>
        FileResponseEntity GetFile(FileRequestEntity fileReqEntity);
        FileResponseEntity GetFile(FileRequestEntity chunkedReqEntity, long offset, long length);
        /// <summary>
        /// Get file by stream
        /// </summary>
        /// <param name="Location"></param>
        /// <param name="DocumentName"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="_dictionary"></param>
        /// <returns></returns>
        Stream GetFile(string Location, string DocumentName, ref WebRequest request, ref WebResponse response, IDictionary<string, object> _dictionary);
        
        /// <summary>
        /// Get presigned URL of a file
        /// </summary>
        /// <param name="reqEntity"></param>
        /// <param name="urlValidityInMinutes"></param>
        /// <returns></returns>
        FileResponseEntity GetFilePreSignedUrl(FileRequestEntity reqEntity, int urlValidityInMinutes);

        FileResponseEntity CreateDirectory(string Location);
        /// <summary>
        /// Delelte Folder for NAS or Delete folder/subfolder for S3 
        /// </summary>
        /// <param name="Location"></param>
        /// <returns></returns>
        FileResponseEntity DeleteDirectory(string Location, bool recursive);
        FileResponseEntity CopyFile(string sourceDir, string destinationDir, string oldFileName, string newFileName);
        FileResponseEntity DeleteFile(string Location, string DocumentName, bool Recursive);
        string GetDataNodeFromNameNode(string Location, string DocumentName, string MethodType, bool needMasterNodeOnly);
        FileResponseEntity CompleteMultiPartUploadS3(string keyName, string folderName, string UploadID, DataSet partETags);
        long GetFileContentLength(string fileLocation, string fileName);
        string GetObjectDetails(string keyName);
        ListObjectsResponse GetDirectoryListing(string Location, string DIR_NAME);
    }
}

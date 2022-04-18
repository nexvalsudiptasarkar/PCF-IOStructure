using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using FileSystemLib.Common;

namespace FileSystemLib.App_Code
{
    internal sealed class FileSysLibUtil : FileSystemBase, IFileSystemLibUtility
    {
        private static IFileSystemLib iFileSourceSystem;
        private static IFileSystemLib iFileTargetSystem;
        private static FileSystemType sourceSysEnum;
        private static FileSystemType targetSysEnum;
                
        #region Constructor
        public FileSysLibUtil(ISharedAppSettings sharedAppSettings)
            : base(sharedAppSettings)
        {
        }
        #endregion

        public FileResponseEntity DownloadFile(string SourceFilePath, string SourceFileName, int SourceStorageId, string TargetFilePath, string TargetFileName, int TargetStorageId)
        {
            FileResponseEntity fileResponse = new FileResponseEntity();
            KeyValuePair<string, string> awsAccessKeys = new KeyValuePair<string, string>(AWSAccessKey, AWSAccessSecretKey);

            try
            {
                WebRequest request = null;
                WebResponse response = null;

                if (sourceSysEnum != getFileSystemEnumByStorageId(SourceStorageId) || iFileSourceSystem == null)
                {
                    sourceSysEnum = getFileSystemEnumByStorageId(SourceStorageId);
                    iFileSourceSystem = FileSystemFactory.GetInstance(_sharedAppSettings, sourceSysEnum);
                }
                IDictionary<string, object> _dictionary = new Dictionary<string, object>();
                using (Stream stream = iFileSourceSystem.GetFile(SourceFilePath, SourceFileName, ref request, ref response, _dictionary))
                {

                    FileRequestEntity fileReqEntity = new FileRequestEntity();
                    fileReqEntity.Stream = stream;
                    fileReqEntity.Location = TargetFilePath;
                    fileReqEntity.OriginalName = TargetFileName;
                    if (targetSysEnum != getFileSystemEnumByStorageId(TargetStorageId) || iFileTargetSystem == null)
                    {
                        targetSysEnum = getFileSystemEnumByStorageId(TargetStorageId);
                        iFileTargetSystem = FileSystemFactory.GetInstance(_sharedAppSettings, sourceSysEnum);
                    }
                    fileReqEntity.FileDictionary = _dictionary;
                    fileResponse = iFileTargetSystem.PutFile(fileReqEntity, default(long), default(long));
                }
                ///_dictionary;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "UploadDownloadHelper.DownloadFile Failed! SourceFilePath{0}, SourceFileName:{1}, SourceStorageId:{2}, TargetFilePath:{3}, TargetFileName:{4}, TargetStorageId:{5}!", SourceFilePath, SourceFileName, SourceStorageId, TargetFilePath, TargetFileName, TargetStorageId);
                throw;
            }
            return fileResponse;
        }

        public FileResponseEntity DownloadFile(string SourceFilePath, string SourceFileName, FileSystemType SourceSystemEnum, string TargetFilePath, string TargetFileName, FileSystemType TargetSystemEnum)
        {
            KeyValuePair<string, string> awsAccessKeys = new KeyValuePair<string, string>(AWSAccessKey, AWSAccessSecretKey);
            FileResponseEntity fileResponse = new FileResponseEntity();
            try
            {
                WebRequest request = null;
                WebResponse response = null;
                if (sourceSysEnum != SourceSystemEnum || iFileSourceSystem == null)
                {
                    sourceSysEnum = SourceSystemEnum;
                    iFileSourceSystem = FileSystemFactory.GetInstance(_sharedAppSettings, sourceSysEnum);
                }
                IDictionary<string, object> _dictionary = new Dictionary<string, object>();
                using (Stream stream = iFileSourceSystem.GetFile(SourceFilePath, SourceFileName, ref request, ref response, _dictionary))
                {

                    FileRequestEntity fileReqEntity = new FileRequestEntity();
                    fileReqEntity.Stream = stream;
                    fileReqEntity.Location = TargetFilePath;
                    fileReqEntity.OriginalName = TargetFileName;
                    if (targetSysEnum != TargetSystemEnum || iFileTargetSystem == null)
                    {
                        targetSysEnum = TargetSystemEnum;
                        iFileTargetSystem = FileSystemFactory.GetInstance(_sharedAppSettings, sourceSysEnum);
                    }
                    fileReqEntity.FileDictionary = _dictionary;
                    fileResponse = iFileTargetSystem.PutFile(fileReqEntity, default(long), default(long));
                }
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "DownloadFile Failed! SourceFilePath{0}, SourceFileName:{1}, TargetFilePath:{2}, TargetFileName:{3}, TargetFileName:{4}!", SourceFilePath, SourceFileName, TargetFilePath, TargetFileName);
                throw;
            }
            return fileResponse;
        }
    }
}

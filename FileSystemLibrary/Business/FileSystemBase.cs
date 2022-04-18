using FileSystemLib.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace FileSystemLib
{
    public abstract class FileSystemBase
    {
        #region Member Variables
        private readonly KeyValuePair<string, string>? _awsAccessKeys;
        protected readonly ISharedAppSettings _sharedAppSettings = null;
        #endregion

        #region Constructor
        protected FileSystemBase(ISharedAppSettings sharedAppSettings)
        {
            _sharedAppSettings = sharedAppSettings;
            if (_sharedAppSettings == null)
            {
                Trace.TraceError("Failed to instantiate 'FileSystemBase', ISharedAppSettings instance is null!");
                throw new ArgumentException(string.Format("Failed to instantiate 'FileSystemBase', ISharedAppSettings instance is null!"));
            }
            _awsAccessKeys = sharedAppSettings.AWSAccessKeys;
        }
        #endregion

        #region Protected Properties
        protected long UploadBufferSize
        {
            get
            {
                long size = _sharedAppSettings.UploadBufferSize;
                if (size > 0)
                {
                    return size;
                }
                return 25165824;
            }
        }

        protected long DownloadBufferSize
        {
            get
            {
                long size = _sharedAppSettings.DownloadBufferSize;
                if (size > 0)
                {
                    return size;
                }
                return 64;
            }
        }

        protected int FileSystemTimeOut
        {
            get
            {
                int value = _sharedAppSettings.FileSystemTimeOutInMS;
                if (value <= 0)
                {
                    return 600000;
                }
                return value;
            }
        }

        protected int WaitDurationBeforeRetryUploadOrDownloadInMs
        {
            get
            {
                int value = _sharedAppSettings.WaitDurationBeforeRetryUploadOrDownloadInMs;
                if (value <= 0)
                {
                    return 1000;
                }
                return value;
            }
        }

        public string AccountNeutralS3BucketName
        {
            get
            {
                return _sharedAppSettings.AccountNeutralS3BucketName;
            }
        }

        protected KeyValuePair<string, string> AwsAccessKeys
        {
            get
            {
                if (_awsAccessKeys == null || _awsAccessKeys.Value.Key == null || _awsAccessKeys.Value.Value == null)
                {
                    Trace.TraceError("Failed to extract Configuration for AWSAccessKey & AWSAccessSecretKey!");
                    throw new ArgumentException(string.Format("Failed to extract Configuration for AWSAccessKey & AWSAccessSecretKey!"));
                }
                return _awsAccessKeys.Value;
            }
        }

        protected string AWSAccessKey
        {
            get
            {
                if (_awsAccessKeys == null || _awsAccessKeys.Value.Key == null || _awsAccessKeys.Value.Value == null)
                {
                    Trace.TraceError("Failed to extract Configuration for AWSAccessKey & AWSAccessSecretKey!");
                    throw new ArgumentException(string.Format("Failed to extract Configuration for AWSAccessKey & AWSAccessSecretKey!"));
                }
                return _awsAccessKeys.Value.Key;
            }
        }

        protected string AWSAccessSecretKey
        {
            get
            {
                if (_awsAccessKeys == null || _awsAccessKeys.Value.Key == null || _awsAccessKeys.Value.Value == null)
                {
                    Trace.TraceError("Failed to extract Configuration for AWSAccessKey & AWSAccessSecretKey!");
                    throw new ArgumentException(string.Format("Failed to extract Configuration for AWSAccessKey & AWSAccessSecretKey!"));
                }
                return _awsAccessKeys.Value.Value;
            }
        }

        protected double MaxDurationInMinuteAfterLastFileAccessForDelete
        {
            get
            {
                int durationInMinute = _sharedAppSettings.MaxDurationInMinuteAfterLastFileAccessForDelete;
                if (durationInMinute <= 0)
                {
                    return 60;
                }
                return durationInMinute;
            }
        }

        protected static FileSystemType getFileSystemEnumByStorageId(int storageId)
        {
            /// TODO: hack! To be fixed by TM, AC..
            if (storageId < 100000) return FileSystemType.NetworkVault;
            if (storageId < 200000) return FileSystemType.AWS_S3;
            if (storageId < 300000) return FileSystemType.HadoopRest;

            FsLogManager.Fatal("FileSystemFactory.GetFileSystemTypeByStorageId: Failed to convert to 'FileSystemEnum' for storageId:{0}!", storageId);
            throw new InvalidOperationException(string.Format("Failed to convert to 'FileSystemEnum' for storageId:{0}!", storageId));
        }
        #endregion
    }
}

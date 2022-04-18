using FileSystemLib.Common;
using System;
using System.Diagnostics;

namespace FileSystemLib
{
    public static class FileSystemFactory
    {
        private static ISharedAppSettings _sharedAppSettings = null;

        public static IFileSystemLib GetInstance(ISharedAppSettings settings, FileSystemType fsType)
        {
            _sharedAppSettings = settings;

            switch (fsType)
            {
                case FileSystemType.NetworkVault:
                    return new NetworkVaultFileSystem(settings);
                case FileSystemType.AWS_S3:
                    return new AwsS3FileSystem(settings);
                case FileSystemType.HadoopRest:
                case FileSystemType.NoImplementation:
                case FileSystemType.WebDav:
                case FileSystemType.Etc:
                default:
                    Trace.TraceInformation("IFileSystemLib of type:{0} is not supported!", fsType);
                    return null;
            }
        }
        public static FileSystemType GetFileSystemTypeByStorageId(int storageId)
        {
            if (storageId < 100000) return FileSystemType.NetworkVault;
            if (storageId < 200000) return FileSystemType.AWS_S3;
            if (storageId < 300000) return FileSystemType.HadoopRest;

            FsLogManager.Fatal("GetFileSystemEnumByStorageId: Failed to convert to 'FileSystemEnum' for storageId:{0}!", storageId);
            throw new InvalidOperationException(string.Format("Failed to convert to 'FileSystemEnum' for storageId:{0}!", storageId));
        }

        public static IS3Ops GetS3Ops(ISharedAppSettings sharedAppSettings)
        {
            return new S3Ops(sharedAppSettings);
        }
    }
}

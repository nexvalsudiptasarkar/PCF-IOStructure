
namespace FileSystemLib
{
    public interface IFileSystemLibUtility
    {
        FileResponseEntity DownloadFile(string SourceFilePath, string SourceFileName, int SourceStorageId, string TargetFilePath, string TargetFileName, int TargetStorageId);
    }
}

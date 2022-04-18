
namespace FileSystemLib
{
    /// <summary>
    /// Utility to locate a file in S3 for a given project, account & storage Id
    /// </summary>
    public static class FileSystemLibFactory
    {
        public static IS3Ops GetS3Ops(ISharedAppSettings sharedAppSettings)
        {
            return new S3Ops(sharedAppSettings);
        }
    }
}
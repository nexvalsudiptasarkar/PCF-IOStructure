
namespace FileSystemLib
{
    public sealed class S3RequestEntity
    {
        public string UploadID { get; set; }
        public int PartNo { get; set; }
        public string FolderLocation { get; set; }
    }
}

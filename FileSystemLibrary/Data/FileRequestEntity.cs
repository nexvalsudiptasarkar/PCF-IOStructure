using System.Collections.Generic;
using System.Web;

namespace FileSystemLib
{
    public class FileRequestEntity
    {
        public string SourceFileName { get; set; }
        public string Location { get; set; }
        public string OriginalName { get; set; }
        public bool Overwrite { get; set; }
        public System.IO.Stream Stream { get; set; }
        public HttpContext httpContext { get; set; }
        public S3RequestEntity S3Entity { get; set; }
        public HadoopRequestEntity HadoopEntity { get; set; }
        public NetworkVaultRequestEntity NetworkEntity { get; set; }
        public IDictionary<string, object> FileDictionary { get; set; }
        public FileRequestEntity()
        {
            S3Entity = new S3RequestEntity();
            HadoopEntity = new HadoopRequestEntity();
            NetworkEntity = new NetworkVaultRequestEntity();
        }
      

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace FileSystemLib
{
    public sealed class FileResponseEntity
    {
        public bool OperationStatus { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public long CompletedByte { get; set; }
        public long ContentLength { get; set; }
        public bool Boolean { get; set; }
        public string ETag { get; set; }
        public int PartNumber { get; set; }
        public long ByteWritten { get; set; }
        public string PresignedURL { get; set; }
    }
  
}

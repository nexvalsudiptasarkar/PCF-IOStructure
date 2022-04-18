using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileSystemLib
{
    internal class ChunkMetaResponse
    {
        public string rootFolder { get; set; }
        public string chunkHeader { get; set; }
        public string chunkHash { get; set; }
    }

    internal sealed class ChunkMetaResponseDetailed
    {
        public string chunksJson { get; set; }
        public int accountId { get; set; }
        public string rootFolder { get; set; }
        public int projectId { get; set; }
        public long fileId { get; set; }
        public long revision { get; set; } 
    }

    internal sealed class GeneralStringResponseList
    {
        [JsonProperty("values")]
        public List<ChunkMetaResponseDetailed> values { get; set; }
    }
}


using System;
using System.Collections.Generic;

namespace FileSystemLib
{
    /// <summary>
    /// Application Settings which are common to Multiple Application & Services. Ideally these are kept in application database centrally & accessed by all background services.
    /// The container (i.e. app & services from which this assembly/library is referenced) should provide the concrete implementation of this.
    /// </summary>
    public interface ISharedAppSettings
    {
        /// <summary>
        /// Storage ID Designated for AWS-S3
        /// </summary>
        int AWSStorageId
        {
            //
            //AWS_S3_STORAGE_ID = 100001;
            //SQL: SELECT StorageID FROM LK_storageinfo where StorageType=0;
            //
            get;
        }

        /// <summary>
        /// AWS Public & Private Keys.
        /// </summary>
        KeyValuePair<string, string>? AWSAccessKeys { get; }

        /// <summary>
        /// File Chunk Service Endpoint (HTTP service)
        /// </summary>
        string FileChunkServiceEndpoint { get; }

        /// <summary>
        /// Command to execute 'GetChunkInfo' command on File Chunk Service Endpoint (HTTP service)
        /// Convention: ServiceEndpoint/Command
        /// </summary>
        string FileChunkServiceGetChunkInfoCmd { get; }

        /// <summary>
        /// Command to execute 'GetPreSignedURL' command on File Chunk Service Endpoint (HTTP service)
        /// Convention: ServiceEndpoint/Command
        /// </summary>
        string PreSignedURLGenerationCmd { get; }

        /// <summary>
        /// Getting configuration entry based on a given key
        /// </summary>
        /// <param name="key">key for which configuration entry needs to be extracted</param>
        /// <returns>null if key is not available; else teh value corresponding to given key</returns>
        string GetConfigSetting(string key);
    }
}

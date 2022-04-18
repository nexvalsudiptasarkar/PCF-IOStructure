using Amazon;
using Amazon.S3;
using FileSystemLib.Common;
using System;
using System.Collections.Generic;

namespace FileSystemLib
{
    internal static class AWS_S3Client
    {
        private static AmazonS3Client _s3Client;

        public static AmazonS3Client GetS3Client(KeyValuePair<string, string>? awsAccessKeys)
        {
            try
            {
                if (_s3Client == null)
                {
                    AmazonS3Config s3config = new AmazonS3Config();
                    s3config.ServiceURL = "Http";
                    _s3Client = new AmazonS3Client(awsAccessKeys.Value.Key, awsAccessKeys.Value.Value, RegionEndpoint.USWest1);
                    //set endpoint as USWest2 as for "Oregon environment": future staging environment 

                    FsLogManager.Info("S3Client [Hash:{0}] instantiated successfully.", _s3Client.GetHashCode());
                }
                return _s3Client;
            }
            catch (AmazonS3Exception ae)
            {
                FsLogManager.Fatal(ae, "Failed to instantiate S3Client!");
                throw;
            }
            catch (Exception ex)
            {
                FsLogManager.Fatal(ex, "Failed to instantiate S3Client!");
                throw;
            }
        }
    }
}
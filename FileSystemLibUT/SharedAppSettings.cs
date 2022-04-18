using FileSystemLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileSystemLibUT
{
    internal sealed class SharedAppSettings : ISharedAppSettings
    {
        #region Data Types
        #endregion

        #region Private Members
        #endregion

        #region Constructor
        public SharedAppSettings()
        {
        }
        #endregion

        #region ISharedAppSettings
        public int AWSStorageId
        {
            get
            {
                //Read: • SELECT StorageID FROM LK_storageinfo where StorageType=0;
                return 100001;
            }
        }

        public KeyValuePair<string, string>? AWSAccessKeys
        {
            get
            {
                string key = ConfigurationManager.AppSettings["AWSAccessKey"];
                string secretKey = ConfigurationManager.AppSettings["AWSAccessSecretKey"];

                return new KeyValuePair<string, string>(key, secretKey);
            }
        }

        public string FileChunkServiceEndpoint
        {
            get { throw new NotImplementedException(); }
        }

        public string FileChunkServiceGetChunkInfoCmd
        {
            get { throw new NotImplementedException(); }
        }

        public string PreSignedURLGenerationCmd
        {
            get { throw new NotImplementedException(); }
        }

        public string GetConfigSetting(string key)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods
        #endregion
    }
}

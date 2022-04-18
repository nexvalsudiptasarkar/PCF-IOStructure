using FileSystemLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileSystemLibUT
{
    internal sealed class S3FileLocator : IS3FileLocator
    {
        #region Data Types
        #endregion

        #region Private Members
        private readonly string _tempBucketName = null;//Temporary variable for test purpose only
        private readonly string _tempRootFolder = null;//Temporary variable for test purpose only
        #endregion

        #region Constructor
        /// <summary>
        /// Temporary Constructor for test purpose only
        /// </summary>
        /// <param name="tempBucketName">Bucket Name</param>
        /// <param name="tempRootFolder">Root Folder</param>
        public S3FileLocator(string tempBucketName, string tempRootFolder)
        {
            _tempBucketName = tempBucketName;
            _tempRootFolder = tempRootFolder;
        }
        #endregion

        #region IS3FileLocator
        public KeyValuePair<string, string>? GetBucketAndFileLocation(int projectId, int accountId, string fileNameInS3WithoutPath)
        {
            if (_tempBucketName != null && _tempRootFolder != null)
            {
                //Temporary implementation for test purpose only
                string s3Key = generatePath(_tempRootFolder, fileNameInS3WithoutPath);
                return new KeyValuePair<string, string>(_tempBucketName, s3Key);
            }
            throw new ArgumentException();
        }

        public KeyValuePair<string, string>? GetBucketAndRootFolderPath(int projectId, int accountId)
        {
            if (_tempBucketName != null && _tempRootFolder != null)
            {
                //Temporary implementation for test purpose only
                string s3Key = generatePath(_tempRootFolder);
                return new KeyValuePair<string, string>(_tempBucketName, s3Key);
            }
            throw new NotImplementedException();
        }
        #endregion

        #region Private Methods
        private string generatePath(params object[] folders)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var o in folders)
            {
                if (o == null)
                {
                    continue;
                }
                string f = o.ToString().Trim();
                if (f.Length > 0 && (f.StartsWith("/") || f.StartsWith("\\")))
                {
                    f = f.Substring(1);
                }
                if (string.IsNullOrWhiteSpace(f))
                {
                    continue;
                }
                //================================================================
                if (sb.Length == 0)
                {
                    sb.Append(f);
                }
                else
                {
                    sb.Append(string.Format("/{0}", f));
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}

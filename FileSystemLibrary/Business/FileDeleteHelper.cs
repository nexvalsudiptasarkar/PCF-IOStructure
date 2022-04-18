using FileSystemLib.Common;
using System;
using System.IO;

namespace FileSystemLib
{
    /// <summary>
    /// Disposer class is used to delete the temp file server location after putting the file to the Hadoop.
    /// </summary>
    internal sealed class FileDeleteHelper : Disposable
    {
        private string _fileToDelete;

        public FileDeleteHelper(string sourceTempFile)
        {
            this._fileToDelete = sourceTempFile;
        }

        protected override void doCleanup()
        {
            try
            {
                File.Delete(_fileToDelete);
            }
            catch (Exception e)
            {
                FsLogManager.Fatal(e, "Failed to delete file:{0}!", _fileToDelete);
            }
        }
    }
}

using Amazon.S3.Model;
using System;

namespace FileSystemLib
{
    public enum FileTransferType { None, Upload, Download };

    public sealed class FileTransferProgressArgs : EventArgs
    {
        #region Private Members
        private readonly TransferProgressArgs _tpa;
        private readonly FileTransferType _tt;
        private readonly string _objectNameForTransfer;
        #endregion

        #region Constructor
        internal FileTransferProgressArgs(FileTransferType tt, string objectNameForTransfer, TransferProgressArgs tpa)
        {
            _tt = tt;
            _tpa = tpa;
            _objectNameForTransfer = objectNameForTransfer;
        }
        #endregion

        #region Public Methods
        public FileTransferType TransferType
        {
            get
            {
                return _tt;
            }
        }
        
        public string ObjectNameForTransfer
        {
            get
            {
                return _objectNameForTransfer;
            }
        }

        public int PercentDone
        {
            get
            {
                return _tpa.PercentDone;
            }
        }

        public long TotalBytes
        {
            get
            {
                return _tpa.TotalBytes;
            }
        }

        public long TransferredBytes
        {
            get
            {
                return _tpa.TransferredBytes;
            }
        }

        public override string ToString()
        {
            return string.Format(" Upload Progress:{0}%, {{2}/{3} bytes transferred]", _tpa.PercentDone, _tpa.TransferredBytes, _tpa.TotalBytes);
        }
        #endregion
    }
}

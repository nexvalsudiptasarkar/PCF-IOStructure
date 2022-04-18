using Nexval.Framework.PCF;
using Nexval.Framework.PCF.Threading;
using FileSystemLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace FileSystemLibUT
{
    public partial class FileSystemLibUTForm : Form
    {
        #region Data Types

        private sealed class UploadRequest
        {
            public UploadRequest(string filePath)
                : this(-1, filePath)
            {
            }

            public UploadRequest(long trackingId, string filePath)
            {
                TrackingId = trackingId;
                FilePath = filePath;

                Status = false;
                FileInfo fi = new FileInfo(FilePath);
                FileSize = fi.Length;
                UpdateTime = DateTime.Now;
            }

            public long TrackingId { get; set; }

            public string FilePath { get; set; }

            public long FileSize { get; set; }

            public bool Status { get; set; }

            public DateTime UpdateTime { get; set; }

            public int RetryCount { get; set; }

            public override string ToString()
            {
                return string.Format("Id:{0}; File:{1}; Status:{2}", TrackingId, FilePath, Status);
            }
        }
        #endregion

        #region Member Variables
        private IS3FileLocator _s3FileLocator = null;
        private readonly ISharedAppSettings _sharedAppSettings;
        private readonly IS3Ops _s3Ops = null;
        private string _defaultS3FolderPath = null;
        private string _defaultS3BucketName = null;
        private string _defaultS3FileName = null;
        private ITaskManager<UploadRequest> _taskManager = null;
        private bool _trackTransferProgress = false;
        #endregion

        public FileSystemLibUTForm()
        {
            _sharedAppSettings = new SharedAppSettings();
            _s3Ops = FileSystemLibFactory.GetS3Ops(_sharedAppSettings);

            _defaultS3BucketName = ConfigurationManager.AppSettings["S3PWCBucketName"];
            _defaultS3FolderPath = ConfigurationManager.AppSettings["S3DefaultFolderPath"];
            _defaultS3FileName = ConfigurationManager.AppSettings["S3DefaultS3FileName"];

            InitializeComponent();
        }

        #region Event Handlers - Controls.
        private void FileSystemLibUTForm_Load(object sender, EventArgs e)
        {
            textBoxBucketName.Text = _defaultS3BucketName;
            textBoxS3RootFolder.Text = _defaultS3FolderPath;
            textBoxS3FileName.Text = _defaultS3FileName;

            textBoxConcurrency.Text = string.Format("{0}", 5);
            checkBoxEnableAutoStop.Checked = true;
            buttonSelectFilesUpload.Enabled = true;
            buttonStop.Enabled = false;
            buttonDownloadFromS3.Enabled = true;
        }

        private void buttonDownloadFromS3_Click(object sender, EventArgs e)
        {
            if (listViewResult.Items.Count <= 0)
            {
                MessageBox.Show(this, "No download requests exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            string defaultS3BucketName = "";
            string defaultS3FolderPath = "";
            string defaultS3FileName = "";
            int concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref  defaultS3BucketName, ref  defaultS3FolderPath, ref  defaultS3FileName, ref  concurrency, ref  enableAutoStop))
            {
                return;
            }
            //initialize(defaultS3BucketName, defaultS3FolderPath, defaultS3FileName, concurrency, enableAutoStop);
            IFileTransferManagerAsync manager = _s3Ops.FileTransferManager;
            manager.OnFileTransferFailed += onFileTransferFailed;
            manager.OnFileTransferSuccessful += onFileTransferSuccessful;

            manager.FileTransferProgressEvent += onFileTransferProgressEvent;

            S3FileLocator locator = new S3FileLocator(defaultS3BucketName, defaultS3FolderPath);
            string tempPath = System.IO.Path.GetTempPath();
            for (int i = 0; i < listViewResult.Items.Count; i++)
            {
                ListViewItem item = listViewResult.Items[i];

                string path = item.SubItems[1].Text;
                string destinationFileInLocalFs = string.Format("{0}{1}", tempPath, Path.GetFileName(path));

                long id = manager.Download(-1, -1, path, locator, destinationFileInLocalFs, _trackTransferProgress);

                item.Tag = id;

                item.SubItems[0].Text = id.ToString();
                item.SubItems[3].Text = "Queued";
            }
        }

        private void buttonEnqueueDownload_Click(object sender, EventArgs e)
        {
            string defaultS3BucketName = "";
            string defaultS3FolderPath = "";
            string defaultS3FileName = "";
            int concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref  defaultS3BucketName, ref  defaultS3FolderPath, ref  defaultS3FileName, ref  concurrency, ref  enableAutoStop))
            {
                return;
            }
            ListViewItem item = new ListViewItem(new string[]
                { 
                    "-1", 
                    defaultS3FileName,
                    "N/A", 
                    "Not Queued yet", 
                    DateTime.Now.ToLongTimeString(),
                    "0"
                });
            item.Tag = defaultS3FileName;
            listViewResult.Items.Add(item);
        }

        private void buttonGenerateURL_Click(object sender, EventArgs e)
        {
            string defaultS3BucketName = "";
            string defaultS3FolderPath = "";
            string defaultS3FileName = "";
            int concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref  defaultS3BucketName, ref  defaultS3FolderPath, ref  defaultS3FileName, ref  concurrency, ref  enableAutoStop))
            {
                return;
            }
            S3FileLocator locator = new S3FileLocator(defaultS3BucketName, defaultS3FolderPath);
            string fileName = textBoxS3FileName.Text;
            if (!string.IsNullOrEmpty(fileName))
            {
                string uri = _s3Ops.GeneratePreSignedDownloadUrl(-1, -1, fileName, locator, new TimeSpan(1, 0, 0));
                string s1 = string.Format("URL Generated for File:{0}, within [Bucket: {1} Path:{2}]\r\n\r\n{3}", fileName, defaultS3BucketName, defaultS3FolderPath, uri);
                textBoxStatus.Text = s1;
                return;
            }
            MessageBox.Show(this, "No file name provided for URL Generation!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);

        }

        private void buttonClearRequests_Click(object sender, EventArgs e)
        {
            listViewResult.Items.Clear();
        }

        private void buttonSelectFilesUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Multiselect = true;
            if (d.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;

            string[] fileNames = d.FileNames;
            listViewResult.Items.Clear();
            for (int i = 0; i < fileNames.Length; i++)
            {
                UploadRequest request = new UploadRequest(-i, fileNames[i]);

                ListViewItem item = new ListViewItem(new string[]
                { 
                    request.TrackingId.ToString(), 
                    request.FilePath,
                    request.FileSize.ToString(), 
                    "Not Queued yet", 
                    request.UpdateTime.ToLongTimeString(),
                    request.RetryCount.ToString()
                });
                item.Tag = request;
                item = listViewResult.Items.Add(item);
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            stopTaskManager();
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            string defaultS3BucketName = "";
            string defaultS3FolderPath = "";
            string defaultS3FileName = "";
            int concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref defaultS3BucketName, ref defaultS3FolderPath, ref defaultS3FileName, ref concurrency, ref enableAutoStop))
            {
                return;
            }

            initialize(defaultS3BucketName, defaultS3FolderPath, defaultS3FileName, concurrency, enableAutoStop);
        }

        private void buttonCopyFile_Click(object sender, EventArgs e)
        {
            string defaultS3BucketName = "";
            string destS3Path = "";
            string defaultS3FolderPath = "";
            string defaultS3FileName = "";
            int concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref  defaultS3BucketName, ref  defaultS3FolderPath, ref  defaultS3FileName, ref  concurrency, ref  enableAutoStop))
            {
                return;
            }
            if (!getUserInputsForDestS3Key(ref  destS3Path))
            {
                return;
            }
            string source = string.Format("{0}/{1}", defaultS3FolderPath, defaultS3FileName);
            textBoxStatus.Text = string.Format("Copying File:{0} to {1} in bucket:{2}...", source, destS3Path, defaultS3BucketName);
            if (_s3Ops.CopyFile(defaultS3BucketName, source, destS3Path))
            {
                string s = textBoxStatus.Text;
                s += string.Format("File:{0} copied to {1} in bucket:{2} successfully.", source, destS3Path, defaultS3BucketName);
                textBoxStatus.Text = s;
                return;
            }
            textBoxStatus.Text = "File copy failed!";
        }

        private void checkBoxTrackProgress_CheckedChanged(object sender, EventArgs e)
        {
            _trackTransferProgress = !_trackTransferProgress;
        }
        #endregion

        private bool getUserInputs(ref string defaultS3BucketName, ref string defaultS3FolderPath, ref string defaultS3FileName, ref int concurrency, ref bool enableAutoStop)
        {
            defaultS3BucketName = textBoxBucketName.Text.Trim();
            if (string.IsNullOrEmpty(defaultS3BucketName) || defaultS3BucketName.Length <= 0)
            {
                MessageBox.Show(this, "Invalid 'Bucket Name' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            defaultS3FolderPath = textBoxS3RootFolder.Text.Trim();
            if (string.IsNullOrEmpty(defaultS3FolderPath) || defaultS3FolderPath.Length <= 0)
            {
                MessageBox.Show(this, "Invalid 'S3Key' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            defaultS3FileName = textBoxS3FileName.Text.Trim();
            if (string.IsNullOrEmpty(defaultS3FileName) || defaultS3FileName.Length <= 0)
            {
                MessageBox.Show(this, "Invalid 'S3-File-Name' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            if (!int.TryParse(textBoxConcurrency.Text, out concurrency))
            {
                MessageBox.Show(this, "Invalid 'Concurrency' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            enableAutoStop = checkBoxEnableAutoStop.Checked;

            _defaultS3BucketName = defaultS3BucketName;
            _defaultS3FolderPath = defaultS3FolderPath;
            return true;
        }

        private bool getUserInputsForDestS3Key(ref string destS3Path)
        {
            destS3Path = textBoxDestinationS3Key.Text.Trim();
            if (string.IsNullOrEmpty(destS3Path) || destS3Path.Length <= 0)
            {
                MessageBox.Show(this, "Invalid 'Destination S3-Path' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            return true;
        }

        private void initialize(string defaultS3BucketName, string defaultS3FolderPath, string defaultS3FileName, int concurrency, bool enableAutoStop)
        {
            stopTaskManager();

            _taskManager = NexvalPcfFactory.GetTaskManager<UploadRequest>(executeRequests, enableAutoStop, "Upload-Test", concurrency, 5);
            _taskManager.OnTaskExecFailed += OnTaskExecFailed;
            _taskManager.OnTaskExecSuccessful += OnTaskExecSuccessful;
            _taskManager.OnStopped += onTaskManagerStopped;

            buttonSelectFilesUpload.Enabled = false;
            buttonUpload.Enabled = false;
            buttonStop.Enabled = true;
            buttonDownloadFromS3.Enabled = false;

            //listViewResult.Items.Clear();
            generateAndEnqueueNewUploadRequests(defaultS3BucketName, defaultS3FolderPath);
            _taskManager.Start();
        }

        private void stopTaskManager()
        {
            if (_taskManager != null)
            {
                if (MessageBox.Show(this, "An Task Manager is active! Would you like to stop this & create a new one?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                _taskManager.OnTaskExecFailed -= OnTaskExecFailed;
                _taskManager.OnTaskExecSuccessful -= OnTaskExecSuccessful;
                _taskManager.Stop();
            }
        }

        private bool executeRequests(long trackingId, UploadRequest request)
        {
            Debug.Assert(_s3FileLocator != null);

            try
            {
                KeyValuePair<string, string>? settings = _s3FileLocator.GetBucketAndFileLocation(-1, -1, Path.GetFileName(request.FilePath));
                _s3Ops.FileTransferProgressEvent += onFileTransferProgressEvent;
                bool b = _s3Ops.UploadFile(-1, -1, Path.GetFileName(request.FilePath), _s3FileLocator, request.FilePath, _trackTransferProgress);

                request.UpdateTime = DateTime.Now;
                return b;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
            }
            return false;
        }

        private void generateAndEnqueueNewUploadRequests(string defaultS3BucketName, string defaultS3FolderPath)
        {
            _s3FileLocator = new S3FileLocator(defaultS3BucketName, defaultS3FolderPath);

            for (int i = 0; i < listViewResult.Items.Count; i++)
            {
                ListViewItem item = listViewResult.Items[i];

                string path = item.SubItems[1].Text;

                UploadRequest request = item.Tag as UploadRequest;
                long id = _taskManager.Enqueue(request);
                request.TrackingId = id;

                item.SubItems[0].Text = id.ToString();
                item.SubItems[3].Text = "Queued";
            }
        }

        private bool updateUploadRequestsInGUI(long trackId, UploadRequest request, bool isSuccessful)
        {
            foreach (ListViewItem lvi in listViewResult.Items)
            {
                if (lvi.Tag == null)
                    continue;

                UploadRequest r = lvi.Tag as UploadRequest;
                if (r.TrackingId == trackId)
                {
                    r.RetryCount++;

                    lvi.Tag = request;
                    lvi.SubItems[4].Text = request.UpdateTime.ToLongTimeString();
                    lvi.SubItems[5].Text = r.RetryCount.ToString();
                    if (isSuccessful)
                    {
                        lvi.SubItems[3].Text = "Done";
                        listViewResult.EnsureVisible(lvi.Index);
                    }
                    else
                    {
                        lvi.SubItems[3].Text = "Failed";
                    }
                    return true;
                }
            }
            return false;
        }

        private bool updateTransferRequestsInGUI(string fileNameForTransfer, int percentComplete)
        {
            foreach (ListViewItem lvi in listViewResult.Items)
            {
                if (lvi.Tag == null)
                    continue;

                string path = lvi.SubItems[1].Text;
                string pathEx = Path.GetFileName(fileNameForTransfer);
                if (path.Equals(pathEx, StringComparison.OrdinalIgnoreCase) || path.Equals(fileNameForTransfer, StringComparison.OrdinalIgnoreCase))
                {
                    lvi.SubItems[3].Text = string.Format("{0}%", percentComplete);
                    return true;
                }
            }
            return false;
        }

        private bool updateDownloadRequestsInGUI(FileTransferRequest request)
        {
            foreach (ListViewItem lvi in listViewResult.Items)
            {
                if (lvi.Tag == null)
                    continue;

                long tid = (long)lvi.Tag;
                if (request.TrackingId == tid)
                {
                    lvi.SubItems[4].Text = request.UpdateTime.ToLongTimeString();
                    lvi.SubItems[5].Text = request.RetryCount.ToString();
                    if (request.Status == RequestStatus.Successful)
                    {
                        FileInfo fi = new FileInfo(request.DiskFilePath);
                        lvi.SubItems[2].Text = fi.Length.ToString();
                        lvi.SubItems[3].Text = "Done";
                        listViewResult.EnsureVisible(lvi.Index);
                    }
                    else
                    {
                        lvi.SubItems[3].Text = "Failed";
                    }
                    return true;
                }
            }
            return false;
        }

        #region Event Handlers - ITaskManager

        private void onFileTransferSuccessful(FileTransferRequest request)
        {
            if (this.listViewResult.InvokeRequired)
            {
                this.listViewResult.BeginInvoke((MethodInvoker)delegate() { updateDownloadRequestsInGUI(request); });
            }
            else
            {
                updateDownloadRequestsInGUI(request);
            }
        }

        private void onFileTransferFailed(FileTransferRequest request)
        {
            if (this.listViewResult.InvokeRequired)
            {
                this.listViewResult.BeginInvoke((MethodInvoker)delegate() { updateDownloadRequestsInGUI(request); });
            }
            else
            {
                updateDownloadRequestsInGUI(request);
            }
        }

        private void OnTaskExecFailed(ITaskManager<UploadRequest> source, long requestTrackingId, UploadRequest request, int retryCount, bool isPermanentlyFailed, ref bool shouldTaskManagerBeTerminated)
        {
            Trace.TraceError("Exec Failed for Task Id:{0}, Data:{1}, Retry Count:{2}!", requestTrackingId, request, retryCount);

            request.UpdateTime = DateTime.Now;
            if (this.listViewResult.InvokeRequired)
            {
                this.listViewResult.BeginInvoke((MethodInvoker)delegate() { updateUploadRequestsInGUI(requestTrackingId, request, false); });
            }
            else
            {
                updateUploadRequestsInGUI(requestTrackingId, request, false);
            }
        }

        private void OnTaskExecSuccessful(ITaskManager<UploadRequest> source, long requestTrackingId, UploadRequest request)
        {
            Trace.TraceInformation("Exec Successful for Task Id:{0}, Data:{1}.", requestTrackingId, request);

            request.UpdateTime = DateTime.Now;
            if (this.listViewResult.InvokeRequired)
            {
                this.listViewResult.BeginInvoke((MethodInvoker)delegate() { updateUploadRequestsInGUI(requestTrackingId, request, true); });
            }
            else
            {
                updateUploadRequestsInGUI(requestTrackingId, request, true);
            }
        }

        private void onTaskManagerStopped(ITaskManager<UploadRequest> source)
        {
            this.buttonSelectFilesUpload.BeginInvoke((MethodInvoker)delegate() { buttonSelectFilesUpload.Enabled = true; });
            this.buttonStop.BeginInvoke((MethodInvoker)delegate() { buttonStop.Enabled = false; });
            this.buttonDownloadFromS3.BeginInvoke((MethodInvoker)delegate() { buttonDownloadFromS3.Enabled = true; });
            this.buttonUpload.BeginInvoke((MethodInvoker)delegate() { buttonUpload.Enabled = true; });

            _taskManager.OnStopped -= onTaskManagerStopped;

            IFileTransferManagerAsync manager = _s3Ops.FileTransferManager;
            manager.OnFileTransferFailed -= onFileTransferFailed;
            manager.OnFileTransferSuccessful -= onFileTransferSuccessful;

            _taskManager = null;
        }

        private void onFileTransferProgressEvent(object sender, FileTransferProgressArgs e)
        {
            if (this.listViewResult.InvokeRequired)
            {
                this.listViewResult.BeginInvoke((MethodInvoker)delegate() { updateTransferRequestsInGUI(e.ObjectNameForTransfer, e.PercentDone); });
            }
            else
            {
                updateTransferRequestsInGUI(e.ObjectNameForTransfer, e.PercentDone);
            }
        }
        #endregion
    }
}

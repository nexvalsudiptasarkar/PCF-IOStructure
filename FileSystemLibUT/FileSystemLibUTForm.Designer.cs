namespace FileSystemLibUT
{
    partial class FileSystemLibUTForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listViewResult = new System.Windows.Forms.ListView();
            this.TrackId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.FilePath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.FileSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Status = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.UpdateTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.RetryCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.checkBoxEnableAutoStop = new System.Windows.Forms.CheckBox();
            this.buttonUpload = new System.Windows.Forms.Button();
            this.textBoxConcurrency = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonDownloadFromS3 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxBucketName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxS3RootFolder = new System.Windows.Forms.TextBox();
            this.buttonSelectFilesUpload = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxS3FileName = new System.Windows.Forms.TextBox();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonEnqueueDownload = new System.Windows.Forms.Button();
            this.buttonClearRequests = new System.Windows.Forms.Button();
            this.buttonGenerateURL = new System.Windows.Forms.Button();
            this.checkBoxTrackProgress = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxDestinationS3Key = new System.Windows.Forms.TextBox();
            this.buttonCopyFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listViewResult
            // 
            this.listViewResult.AutoArrange = false;
            this.listViewResult.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.TrackId,
            this.FilePath,
            this.FileSize,
            this.Status,
            this.UpdateTime,
            this.RetryCount});
            this.listViewResult.FullRowSelect = true;
            this.listViewResult.Location = new System.Drawing.Point(9, 33);
            this.listViewResult.MultiSelect = false;
            this.listViewResult.Name = "listViewResult";
            this.listViewResult.Size = new System.Drawing.Size(699, 158);
            this.listViewResult.TabIndex = 16;
            this.listViewResult.UseCompatibleStateImageBehavior = false;
            this.listViewResult.View = System.Windows.Forms.View.Details;
            // 
            // TrackId
            // 
            this.TrackId.Text = "Track Id";
            // 
            // FilePath
            // 
            this.FilePath.Text = "Input File Path";
            this.FilePath.Width = 240;
            // 
            // FileSize
            // 
            this.FileSize.Text = "File Size (Bytes)";
            this.FileSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.FileSize.Width = 120;
            // 
            // Status
            // 
            this.Status.Text = "Status";
            // 
            // UpdateTime
            // 
            this.UpdateTime.Text = "Last Update Time";
            this.UpdateTime.Width = 150;
            // 
            // RetryCount
            // 
            this.RetryCount.Text = "Retry Count";
            // 
            // checkBoxEnableAutoStop
            // 
            this.checkBoxEnableAutoStop.AutoSize = true;
            this.checkBoxEnableAutoStop.Location = new System.Drawing.Point(12, 8);
            this.checkBoxEnableAutoStop.Name = "checkBoxEnableAutoStop";
            this.checkBoxEnableAutoStop.Size = new System.Drawing.Size(96, 17);
            this.checkBoxEnableAutoStop.TabIndex = 15;
            this.checkBoxEnableAutoStop.Text = "Auto Stop PCF";
            this.checkBoxEnableAutoStop.UseVisualStyleBackColor = true;
            // 
            // buttonUpload
            // 
            this.buttonUpload.Location = new System.Drawing.Point(593, 4);
            this.buttonUpload.Name = "buttonUpload";
            this.buttonUpload.Size = new System.Drawing.Size(58, 22);
            this.buttonUpload.TabIndex = 14;
            this.buttonUpload.Text = "&Upload";
            this.buttonUpload.UseVisualStyleBackColor = true;
            this.buttonUpload.Click += new System.EventHandler(this.buttonUpload_Click);
            // 
            // textBoxConcurrency
            // 
            this.textBoxConcurrency.Location = new System.Drawing.Point(300, 7);
            this.textBoxConcurrency.MaxLength = 2;
            this.textBoxConcurrency.Name = "textBoxConcurrency";
            this.textBoxConcurrency.Size = new System.Drawing.Size(45, 20);
            this.textBoxConcurrency.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(224, 10);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Concurrency:";
            // 
            // buttonDownloadFromS3
            // 
            this.buttonDownloadFromS3.Location = new System.Drawing.Point(599, 202);
            this.buttonDownloadFromS3.Name = "buttonDownloadFromS3";
            this.buttonDownloadFromS3.Size = new System.Drawing.Size(109, 23);
            this.buttonDownloadFromS3.TabIndex = 14;
            this.buttonDownloadFromS3.Text = "Download From S3";
            this.buttonDownloadFromS3.UseVisualStyleBackColor = true;
            this.buttonDownloadFromS3.Click += new System.EventHandler(this.buttonDownloadFromS3_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 204);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "S3 Bucket Name:";
            // 
            // textBoxBucketName
            // 
            this.textBoxBucketName.Location = new System.Drawing.Point(101, 204);
            this.textBoxBucketName.MaxLength = 255;
            this.textBoxBucketName.Name = "textBoxBucketName";
            this.textBoxBucketName.Size = new System.Drawing.Size(213, 20);
            this.textBoxBucketName.TabIndex = 13;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 234);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "S3 Root Folder:";
            // 
            // textBoxS3RootFolder
            // 
            this.textBoxS3RootFolder.Location = new System.Drawing.Point(101, 231);
            this.textBoxS3RootFolder.MaxLength = 255;
            this.textBoxS3RootFolder.Name = "textBoxS3RootFolder";
            this.textBoxS3RootFolder.Size = new System.Drawing.Size(363, 20);
            this.textBoxS3RootFolder.TabIndex = 13;
            // 
            // buttonSelectFilesUpload
            // 
            this.buttonSelectFilesUpload.Location = new System.Drawing.Point(445, 4);
            this.buttonSelectFilesUpload.Name = "buttonSelectFilesUpload";
            this.buttonSelectFilesUpload.Size = new System.Drawing.Size(144, 22);
            this.buttonSelectFilesUpload.TabIndex = 14;
            this.buttonSelectFilesUpload.Text = "Select &Files to Upload...";
            this.buttonSelectFilesUpload.UseVisualStyleBackColor = true;
            this.buttonSelectFilesUpload.Click += new System.EventHandler(this.buttonSelectFilesUpload_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(470, 234);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "S3 File Name:";
            // 
            // textBoxS3FileName
            // 
            this.textBoxS3FileName.Location = new System.Drawing.Point(549, 231);
            this.textBoxS3FileName.MaxLength = 255;
            this.textBoxS3FileName.Name = "textBoxS3FileName";
            this.textBoxS3FileName.Size = new System.Drawing.Size(146, 20);
            this.textBoxS3FileName.TabIndex = 13;
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.Location = new System.Drawing.Point(17, 284);
            this.textBoxStatus.Multiline = true;
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.Size = new System.Drawing.Size(691, 56);
            this.textBoxStatus.TabIndex = 17;
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(656, 3);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(55, 23);
            this.buttonStop.TabIndex = 18;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonEnqueueDownload
            // 
            this.buttonEnqueueDownload.Location = new System.Drawing.Point(454, 202);
            this.buttonEnqueueDownload.Name = "buttonEnqueueDownload";
            this.buttonEnqueueDownload.Size = new System.Drawing.Size(128, 23);
            this.buttonEnqueueDownload.TabIndex = 19;
            this.buttonEnqueueDownload.Text = "Enqueue Download";
            this.buttonEnqueueDownload.UseVisualStyleBackColor = true;
            this.buttonEnqueueDownload.Click += new System.EventHandler(this.buttonEnqueueDownload_Click);
            // 
            // buttonClearRequests
            // 
            this.buttonClearRequests.Location = new System.Drawing.Point(348, 4);
            this.buttonClearRequests.Name = "buttonClearRequests";
            this.buttonClearRequests.Size = new System.Drawing.Size(91, 23);
            this.buttonClearRequests.TabIndex = 20;
            this.buttonClearRequests.Text = "Clear Requests";
            this.buttonClearRequests.UseVisualStyleBackColor = true;
            this.buttonClearRequests.Click += new System.EventHandler(this.buttonClearRequests_Click);
            // 
            // buttonGenerateURL
            // 
            this.buttonGenerateURL.Location = new System.Drawing.Point(320, 202);
            this.buttonGenerateURL.Name = "buttonGenerateURL";
            this.buttonGenerateURL.Size = new System.Drawing.Size(128, 23);
            this.buttonGenerateURL.TabIndex = 19;
            this.buttonGenerateURL.Text = "Generate URL";
            this.buttonGenerateURL.UseVisualStyleBackColor = true;
            this.buttonGenerateURL.Click += new System.EventHandler(this.buttonGenerateURL_Click);
            // 
            // checkBoxTrackProgress
            // 
            this.checkBoxTrackProgress.AutoSize = true;
            this.checkBoxTrackProgress.Location = new System.Drawing.Point(109, 8);
            this.checkBoxTrackProgress.Name = "checkBoxTrackProgress";
            this.checkBoxTrackProgress.Size = new System.Drawing.Size(98, 17);
            this.checkBoxTrackProgress.TabIndex = 21;
            this.checkBoxTrackProgress.Text = "Track Progress";
            this.checkBoxTrackProgress.UseVisualStyleBackColor = true;
            this.checkBoxTrackProgress.CheckedChanged += new System.EventHandler(this.checkBoxTrackProgress_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 260);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(142, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Destination S3-Key for Copy:";
            // 
            // textBoxDestinationS3Key
            // 
            this.textBoxDestinationS3Key.Location = new System.Drawing.Point(162, 257);
            this.textBoxDestinationS3Key.MaxLength = 255;
            this.textBoxDestinationS3Key.Name = "textBoxDestinationS3Key";
            this.textBoxDestinationS3Key.Size = new System.Drawing.Size(431, 20);
            this.textBoxDestinationS3Key.TabIndex = 13;
            // 
            // buttonCopyFile
            // 
            this.buttonCopyFile.Location = new System.Drawing.Point(599, 255);
            this.buttonCopyFile.Name = "buttonCopyFile";
            this.buttonCopyFile.Size = new System.Drawing.Size(109, 23);
            this.buttonCopyFile.TabIndex = 14;
            this.buttonCopyFile.Text = "Copy File";
            this.buttonCopyFile.UseVisualStyleBackColor = true;
            this.buttonCopyFile.Click += new System.EventHandler(this.buttonCopyFile_Click);
            // 
            // FileSystemLibUTForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 420);
            this.Controls.Add(this.checkBoxTrackProgress);
            this.Controls.Add(this.buttonClearRequests);
            this.Controls.Add(this.buttonGenerateURL);
            this.Controls.Add(this.buttonEnqueueDownload);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.textBoxStatus);
            this.Controls.Add(this.listViewResult);
            this.Controls.Add(this.checkBoxEnableAutoStop);
            this.Controls.Add(this.buttonSelectFilesUpload);
            this.Controls.Add(this.buttonCopyFile);
            this.Controls.Add(this.buttonDownloadFromS3);
            this.Controls.Add(this.buttonUpload);
            this.Controls.Add(this.textBoxDestinationS3Key);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxS3RootFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxS3FileName);
            this.Controls.Add(this.textBoxBucketName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxConcurrency);
            this.Controls.Add(this.label4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FileSystemLibUTForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "File System Library UT";
            this.Load += new System.EventHandler(this.FileSystemLibUTForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listViewResult;
        private System.Windows.Forms.ColumnHeader TrackId;
        private System.Windows.Forms.ColumnHeader FilePath;
        private System.Windows.Forms.ColumnHeader FileSize;
        private System.Windows.Forms.ColumnHeader Status;
        private System.Windows.Forms.ColumnHeader UpdateTime;
        private System.Windows.Forms.CheckBox checkBoxEnableAutoStop;
        private System.Windows.Forms.Button buttonUpload;
        private System.Windows.Forms.TextBox textBoxConcurrency;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonDownloadFromS3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxBucketName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxS3RootFolder;
        private System.Windows.Forms.Button buttonSelectFilesUpload;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxS3FileName;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.ColumnHeader RetryCount;
        private System.Windows.Forms.Button buttonEnqueueDownload;
        private System.Windows.Forms.Button buttonClearRequests;
        private System.Windows.Forms.Button buttonGenerateURL;
        private System.Windows.Forms.CheckBox checkBoxTrackProgress;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxDestinationS3Key;
        private System.Windows.Forms.Button buttonCopyFile;
    }
}


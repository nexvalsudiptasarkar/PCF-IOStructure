namespace NexvalPcfTestApplication
{
    partial class FormFactorialUsingPcf
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxStartNumber = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxEndNumber = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxCount = new System.Windows.Forms.TextBox();
            this.buttonExecute = new System.Windows.Forms.Button();
            this.checkBoxEnableAutoStop = new System.Windows.Forms.CheckBox();
            this.listViewResult = new System.Windows.Forms.ListView();
            this.TrackingId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Input = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Output = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.UpdateTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxConcurrency = new System.Windows.Forms.TextBox();
            this.buttonEnqueue = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Start Number:";
            // 
            // textBoxStartNumber
            // 
            this.textBoxStartNumber.Location = new System.Drawing.Point(88, 6);
            this.textBoxStartNumber.MaxLength = 2;
            this.textBoxStartNumber.Name = "textBoxStartNumber";
            this.textBoxStartNumber.Size = new System.Drawing.Size(45, 20);
            this.textBoxStartNumber.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(139, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "End Number:";
            // 
            // textBoxEndNumber
            // 
            this.textBoxEndNumber.Location = new System.Drawing.Point(212, 7);
            this.textBoxEndNumber.MaxLength = 2;
            this.textBoxEndNumber.Name = "textBoxEndNumber";
            this.textBoxEndNumber.Size = new System.Drawing.Size(45, 20);
            this.textBoxEndNumber.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(263, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Count:";
            // 
            // textBoxCount
            // 
            this.textBoxCount.Location = new System.Drawing.Point(307, 9);
            this.textBoxCount.MaxLength = 4;
            this.textBoxCount.Name = "textBoxCount";
            this.textBoxCount.Size = new System.Drawing.Size(44, 20);
            this.textBoxCount.TabIndex = 1;
            // 
            // buttonExecute
            // 
            this.buttonExecute.Location = new System.Drawing.Point(173, 244);
            this.buttonExecute.Name = "buttonExecute";
            this.buttonExecute.Size = new System.Drawing.Size(128, 22);
            this.buttonExecute.TabIndex = 3;
            this.buttonExecute.Text = "Execute";
            this.buttonExecute.UseVisualStyleBackColor = true;
            this.buttonExecute.Click += new System.EventHandler(this.buttonExecute_Click);
            // 
            // checkBoxEnableAutoStop
            // 
            this.checkBoxEnableAutoStop.AutoSize = true;
            this.checkBoxEnableAutoStop.Location = new System.Drawing.Point(15, 38);
            this.checkBoxEnableAutoStop.Name = "checkBoxEnableAutoStop";
            this.checkBoxEnableAutoStop.Size = new System.Drawing.Size(96, 17);
            this.checkBoxEnableAutoStop.TabIndex = 4;
            this.checkBoxEnableAutoStop.Text = "Auto Stop PCF";
            this.checkBoxEnableAutoStop.UseVisualStyleBackColor = true;
            // 
            // listViewResult
            // 
            this.listViewResult.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.TrackingId,
            this.Input,
            this.Output,
            this.UpdateTime});
            this.listViewResult.Location = new System.Drawing.Point(15, 62);
            this.listViewResult.Name = "listViewResult";
            this.listViewResult.Size = new System.Drawing.Size(454, 176);
            this.listViewResult.TabIndex = 5;
            this.listViewResult.UseCompatibleStateImageBehavior = false;
            this.listViewResult.View = System.Windows.Forms.View.Details;
            // 
            // TrackingId
            // 
            this.TrackingId.Text = "Tracking Id";
            this.TrackingId.Width = 90;
            // 
            // Input
            // 
            this.Input.Text = "Input";
            // 
            // Output
            // 
            this.Output.Text = "Factorial";
            this.Output.Width = 120;
            // 
            // UpdateTime
            // 
            this.UpdateTime.Text = "Last Update Time";
            this.UpdateTime.Width = 150;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(139, 39);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(93, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Max Concurrency:";
            // 
            // textBoxConcurrency
            // 
            this.textBoxConcurrency.Location = new System.Drawing.Point(238, 36);
            this.textBoxConcurrency.MaxLength = 2;
            this.textBoxConcurrency.Name = "textBoxConcurrency";
            this.textBoxConcurrency.Size = new System.Drawing.Size(45, 20);
            this.textBoxConcurrency.TabIndex = 1;
            // 
            // buttonEnqueue
            // 
            this.buttonEnqueue.Location = new System.Drawing.Point(357, 7);
            this.buttonEnqueue.Name = "buttonEnqueue";
            this.buttonEnqueue.Size = new System.Drawing.Size(63, 22);
            this.buttonEnqueue.TabIndex = 3;
            this.buttonEnqueue.Text = "Enqueue";
            this.buttonEnqueue.UseVisualStyleBackColor = true;
            this.buttonEnqueue.Click += new System.EventHandler(this.buttonEnqueue_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(426, 7);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(43, 22);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // FormFactorialUsingPcf
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 271);
            this.Controls.Add(this.listViewResult);
            this.Controls.Add(this.checkBoxEnableAutoStop);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonEnqueue);
            this.Controls.Add(this.buttonExecute);
            this.Controls.Add(this.textBoxCount);
            this.Controls.Add(this.textBoxConcurrency);
            this.Controls.Add(this.textBoxEndNumber);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxStartNumber);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormFactorialUsingPcf";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Test PCF";
            this.Load += new System.EventHandler(this.FormFactorialUsingPcf_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxStartNumber;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxEndNumber;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxCount;
        private System.Windows.Forms.Button buttonExecute;
        private System.Windows.Forms.CheckBox checkBoxEnableAutoStop;
        private System.Windows.Forms.ListView listViewResult;
        private System.Windows.Forms.ColumnHeader TrackingId;
        private System.Windows.Forms.ColumnHeader Input;
        private System.Windows.Forms.ColumnHeader Output;
        private System.Windows.Forms.ColumnHeader UpdateTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxConcurrency;
        private System.Windows.Forms.Button buttonEnqueue;
        private System.Windows.Forms.Button buttonStop;
    }
}


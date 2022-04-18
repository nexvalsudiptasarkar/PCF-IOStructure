using Nexval.Framework.PCF;
using Nexval.Framework.PCF.Threading;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace NexvalPcfTestApplication
{
    public partial class FormFactorialUsingPcf : Form
    {
        #region Data Types

        private sealed class FactorialRequest
        {
            public FactorialRequest(int v)
                : this(-1, v)
            {
            }

            public FactorialRequest(long id, int v)
            {
                Id = id;
                Input = v;
                Output = string.Empty;
                UpdateTime = DateTime.Now;
            }

            public long Id { get; set; }

            public int Input { get; set; }

            public string Output { get; set; }

            public DateTime UpdateTime { get; set; }

            public override string ToString()
            {
                return string.Format("Id:{0}; Input:{1}; Output:{2}", Id, Input, Output);
            }
        }
        #endregion

        #region Member Variables
        private ITaskManager<FactorialRequest> _taskManager = null;
        private static Func<int, int> Factorial = x => x < 0 ? -1 : x == 1 || x == 0 ? 1 : x * Factorial(x - 1);
        #endregion

        public FormFactorialUsingPcf()
        {
            InitializeComponent();
        }

        private void FormFactorialUsingPcf_Load(object sender, EventArgs e)
        {
            textBoxCount.Text = "200";
            textBoxStartNumber.Text = "5";
            textBoxEndNumber.Text = "15";
            textBoxConcurrency.Text = "5";
            checkBoxEnableAutoStop.Checked = true;
            listViewResult.Items.Clear();
            buttonEnqueue.Enabled = false;
            buttonStop.Enabled = false;
        }

        private void buttonExecute_Click(object sender, EventArgs e)
        {
            int count = -1, startNumber = -1, endNumber = -1, concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref count, ref startNumber, ref endNumber, ref concurrency, ref enableAutoStop))
                return;

            initialize(count, startNumber, endNumber, concurrency, enableAutoStop);
        }

        private void buttonEnqueue_Click(object sender, EventArgs e)
        {
            if (_taskManager == null || _taskManager.IsStopped)
            {
                MessageBox.Show(this, "Task Manager is not in running state!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            int count = -1, startNumber = -1, endNumber = -1, concurrency = -1;
            bool enableAutoStop = false;

            if (!getUserInputs(ref count, ref startNumber, ref endNumber, ref concurrency, ref enableAutoStop))
                return;

            generateAndEnqueueNewRequests(count, startNumber, endNumber);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (_taskManager != null || !_taskManager.IsStopped)
            {
                stopTaskManager();
                return;
            }
        }

        #region Private Methods
        private bool getUserInputs(ref int count, ref int startNumber, ref int endNumber, ref int concurrency, ref bool enableAutoStop)
        {
            if (!int.TryParse(textBoxCount.Text, out count))
            {
                MessageBox.Show(this, "Invalid 'Count' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            if (!int.TryParse(textBoxStartNumber.Text, out startNumber))
            {
                MessageBox.Show(this, "Invalid 'Start Number' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            if (!int.TryParse(textBoxEndNumber.Text, out endNumber))
            {
                MessageBox.Show(this, "Invalid 'End Number' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            if (!int.TryParse(textBoxConcurrency.Text, out concurrency))
            {
                MessageBox.Show(this, "Invalid 'Concurrency' specified!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            enableAutoStop = checkBoxEnableAutoStop.Checked;
            return true;
        }

        private void initialize(int count, int startNumber, int endNumber, int concurrency, bool enableAutoStop)
        {
            stopTaskManager();

            _taskManager = NexvalPcfFactory.GetTaskManager<FactorialRequest>(executeRequests, enableAutoStop, "Factorial-Test", concurrency, 3);
            _taskManager.OnTaskExecFailed += OnTaskExecFailed;
            _taskManager.OnTaskExecSuccessful += OnTaskExecSuccessful;
            _taskManager.OnStopped += onTaskManagerStopped;

            buttonEnqueue.Enabled = true;
            buttonStop.Enabled = true;

            listViewResult.Items.Clear();
            generateAndEnqueueNewRequests(count, startNumber, endNumber);
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
                _taskManager.Dispose();
            }
        }

        private bool updateListViewItem(long trackId, FactorialRequest request)
        {
            foreach (ListViewItem lvi in listViewResult.Items)
            {
                if (lvi.Tag == null)
                    continue;

                FactorialRequest r = lvi.Tag as FactorialRequest;
                if (r.Id == trackId)
                {
                    lvi.Tag = request;
                    lvi.SubItems[2].Text = request.Output.ToString();
                    lvi.SubItems[3].Text = request.UpdateTime.ToLongTimeString();
                    listViewResult.EnsureVisible(lvi.Index);
                    return true;
                }
            }
            return false;
        }

        private bool executeRequests(long trackingId, FactorialRequest request)
        {
            try
            {
                int v = request.Input;
                string result = DateTime.Now.ToString();
                request.Output = result;
                Trace.TraceInformation("Result: {0}", request);
                return true;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
            }
            return false;
        }

        private void generateAndEnqueueNewRequests(int count, int startNumber, int endNumber)
        {
            Random r = new Random();
            for (int i = 0; i < count; i++)
            {
                int v = r.Next(startNumber, endNumber);
                FactorialRequest request = new FactorialRequest(v);
                long id = _taskManager.Enqueue(request);
                request.Id = id;

                ListViewItem item = new ListViewItem(new string[] { request.Id.ToString(), request.Input.ToString(), request.Output.ToString(), request.UpdateTime.ToLongTimeString() });
                item.Tag = request;
                item = listViewResult.Items.Add(item);
            }
        }

        private void OnTaskExecFailed(ITaskManager<FactorialRequest> source, long requestTrackingId, FactorialRequest request, int retryCount, bool isPermanentlyFailed, ref bool shouldTaskManagerBeTerminated)
        {
            Trace.TraceError("Exec Failed for Task Id:{0}, Data:{1}, Retry Count:{2}!", requestTrackingId, request, retryCount);
        }

        private void OnTaskExecSuccessful(ITaskManager<FactorialRequest> source, long requestTrackingId, FactorialRequest request)
        {
            Trace.TraceInformation("Exec Successful for Task Id:{0}, Data:{1}.", requestTrackingId, request);

            request.UpdateTime = DateTime.Now;
            if (this.listViewResult.InvokeRequired)
            {
                this.listViewResult.BeginInvoke((MethodInvoker)delegate() { updateListViewItem(requestTrackingId, request); });
            }
            else
            {
                updateListViewItem(requestTrackingId, request);
            }
        }

        private void onTaskManagerStopped(ITaskManager<FormFactorialUsingPcf.FactorialRequest> source)
        {
            this.buttonEnqueue.BeginInvoke((MethodInvoker)delegate() { buttonEnqueue.Enabled = false; });
            this.buttonStop.BeginInvoke((MethodInvoker)delegate() { buttonStop.Enabled = false; });
            _taskManager.OnStopped -= onTaskManagerStopped;
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileSystemLibUT
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.ServicePointManager.MaxServicePointIdleTime = 60 * 60 * 1000;  //System.Threading.Timeout.Infinite;
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            System.Net.ServicePointManager.SetTcpKeepAlive(false, 5000, 100);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FileSystemLibUTForm());
        }
    }
}

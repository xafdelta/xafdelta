using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.ExpressApp.Utils;
using XafDelta.Win.Properties;

namespace XafDelta.Win
{
    public partial class ProgressForm : Form
    {
        public BackgroundWorker Worker { get; private set; }

        public ProgressForm(BackgroundWorker worker)
        {
            InitializeComponent();
            Worker = worker;
            if (Worker != null)
                Worker.RunWorkerCompleted += workerRunWorkerCompleted;
        }

        void workerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnStop.Text = CaptionHelper.GetLocalizedText("DialogButtons", "Close"); 
        }

        private void btnStopClick(object sender, EventArgs e)
        {
            if(Worker.IsBusy)
                Worker.CancelAsync();
            else
                Close();
        }

        public void AddProgessText(string text, int percent)
        {
            if (rtbStatus.Lines.Length == 0)
            {
                rtbStatus.SelectionColor = Color.DarkBlue;
                rtbStatus.AppendText(Resources.ProgressForm_Powered +
                    Assembly.GetExecutingAssembly().GetName().Version + '\n');
                rtbStatus.SelectionColor = Color.DarkBlue;
                rtbStatus.AppendText(Resources.ProgressForm_SeeSite + '\n');
                rtbStatus.AppendText(""+'\n');
                rtbStatus.SelectionColor = SystemColors.WindowText;
            }

            var messageColor = Color.FromArgb(percent);
            rtbStatus.SelectionColor = messageColor;
            rtbStatus.AppendText(DateTime.Now + " " + text + '\n');

            if (messageColor != SystemColors.WindowText)
                rtbStatus.SelectionColor = SystemColors.WindowText;
            lblStatus.Text = "";
        }

        private static void gotoWebsite()
        {
            Process.Start(@"http:\\xafdelta.narod.ru");
        }

        private void rtbStatusLinkClicked(object sender, LinkClickedEventArgs e)
        {
            gotoWebsite();
        }

        private void ProgressForm_Load(object sender, EventArgs e)
        {
            Text = CaptionHelper.GetLocalizedText("Replication", "Replication"); 
            btnStop.Text = CaptionHelper.GetLocalizedText("DialogButtons", "Abort");
        }

        public void ShowPercent(int progressPercentage)
        {
            prbProgress.Value = progressPercentage;
        }

        public void ShowStatus(string statusText)
        {
            lblStatus.Text = statusText;
        }
    }
}

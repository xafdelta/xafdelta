using System;
using System.ComponentModel;
using System.Drawing;

namespace XafDelta
{
    /// <summary>
    /// Task executor proxy.
    /// For internal use only.
    /// </summary>
    public class ActionWorker
    {
        /// <summary>
        /// Gets the worker.
        /// </summary>
        public BackgroundWorker Worker { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionWorker"/> class for synchronous task execution.
        /// </summary>
        public ActionWorker() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionWorker"/> class for asynchronous task execution.
        /// </summary>
        /// <param name="worker">The worker.</param>
        public ActionWorker(BackgroundWorker worker)
        {
            Worker = worker;
        }

        /// <summary>
        /// Reports the progress.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="messageText">The message text</param>
        /// <param name="args">The message arguments</param>
        public void ReportProgress(Color color, string messageText, params object[] args)
        {
            if(Worker != null)
                Worker.ReportProgress(color.ToArgb(), string.Format(messageText, args));
        }

        /// <summary>
        /// Reports the progress with default color.
        /// </summary>
        /// <param name="messageText">The message text.</param>
        /// <param name="args">The arguments</param>
        public void ReportProgress(string messageText, params object[] args)
        {
            ReportProgress(SystemColors.WindowText, messageText, args);
        }

        /// <summary>
        /// Reports the error.
        /// </summary>
        /// <param name="messageText">The message text.</param>
        /// <param name="args">The arguments.</param>
        public void ReportError(string messageText, params object[] args)
        {
            ReportProgress(Color.Red, messageText, args);
        }

        /// <summary>
        /// Reports the percent.
        /// </summary>
        /// <param name="percentValue">The percent value.</param>
        public void ReportPercent(double percentValue)
        {
            if (Worker != null)
            {
                var intPercent = (int) Math.Round(percentValue*100, 0);
                if(intPercent<0) 
                    intPercent = 0;
                if(intPercent>100) 
                    intPercent = 100;
                Worker.ReportProgress(intPercent, null);
            }
        }

        /// <summary>
        /// Color code for status messages
        /// </summary>
        public static int StatusCode = 0x8E5DD69;

        /// <summary>
        /// Shows the status.
        /// </summary>
        /// <param name="statusText">The status text.</param>
        public void ShowStatus(string statusText)
        {
            if (Worker != null)
                Worker.ReportProgress(StatusCode, statusText);
        }

        /// <summary>
        /// Gets a value indicating whether the application has requested cancellation of a background operation.
        /// </summary>
        /// <value>
        ///   <c>true</c> if cancellation pending; otherwise, <c>false</c>.
        /// </value>
        public bool CancellationPending { get { return Worker != null && Worker.CancellationPending; }}
    }

}
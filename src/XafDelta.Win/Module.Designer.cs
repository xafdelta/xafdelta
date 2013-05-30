namespace XafDelta.Win
{
    partial class XafDeltaWinModule
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
                foreach (var worker in workers)
                {
                    if(worker.Value.IsBusy)
                        worker.Value.CancelAsync();
                    worker.Value.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // XafDeltaWinModule
            // 
            this.RequiredModuleTypes.Add(typeof(XafDelta.XafDeltaModule));

        }

        #endregion
    }
}

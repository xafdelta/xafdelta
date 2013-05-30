namespace XafDelta.Delivery
{
    partial class VcDelivery
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.DownloadMessagesAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.UploadMessagesAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            // 
            // DownloadMessagesAction
            // 
            this.DownloadMessagesAction.Caption = "Download messages";
            this.DownloadMessagesAction.Category = "RecordEdit";
            this.DownloadMessagesAction.ConfirmationMessage = "Download XafDelta messages ?";
            this.DownloadMessagesAction.Id = "DownloadMessagesAction";
            this.DownloadMessagesAction.ImageName = "DownloadMessages";
            this.DownloadMessagesAction.Shortcut = null;
            this.DownloadMessagesAction.Tag = null;
            this.DownloadMessagesAction.TargetObjectsCriteria = null;
            this.DownloadMessagesAction.TargetObjectType = typeof(XafDelta.Messaging.Package);
            this.DownloadMessagesAction.TargetViewId = null;
            this.DownloadMessagesAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.DownloadMessagesAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.DownloadMessagesAction.ToolTip = "Download messages";
            this.DownloadMessagesAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.DownloadMessagesAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.DownloadMessagesAction_Execute);
            // 
            // UploadMessagesAction
            // 
            this.UploadMessagesAction.Caption = "Upload messages";
            this.UploadMessagesAction.Category = "RecordEdit";
            this.UploadMessagesAction.ConfirmationMessage = "Upload XafDelta messages ?";
            this.UploadMessagesAction.Id = "UploadMessagesAction";
            this.UploadMessagesAction.ImageName = "UploadMessages";
            this.UploadMessagesAction.Shortcut = null;
            this.UploadMessagesAction.Tag = null;
            this.UploadMessagesAction.TargetObjectsCriteria = null;
            this.UploadMessagesAction.TargetObjectType = typeof(XafDelta.Messaging.Package);
            this.UploadMessagesAction.TargetViewId = null;
            this.UploadMessagesAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.UploadMessagesAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.UploadMessagesAction.ToolTip = "Upload XafDelta messages";
            this.UploadMessagesAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.UploadMessagesAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.UploadMessagesAction_Execute);

        }

        #endregion

        public DevExpress.ExpressApp.Actions.SimpleAction DownloadMessagesAction;
        public DevExpress.ExpressApp.Actions.SimpleAction UploadMessagesAction;

    }
}

namespace XafDelta
{
    partial class VcActions
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
            this.BuildPackageAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.LoadAllAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.LoadPackageAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.BuildSnapshotAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.ExportFilesAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.ImportFilesAction = new DevExpress.ExpressApp.Actions.PopupWindowShowAction(this.components);
            this.WinImportFilesAction = new DevExpress.ExpressApp.Actions.SimpleAction(this.components);
            this.BuildSnapshotActionPop = new DevExpress.ExpressApp.Actions.PopupWindowShowAction(this.components);
            // 
            // BuildPackageAction
            // 
            this.BuildPackageAction.Caption = "Build packages";
            this.BuildPackageAction.Category = "RecordEdit";
            this.BuildPackageAction.ConfirmationMessage = "Create new packages for protocol records ?";
            this.BuildPackageAction.Id = "BuildPackageAction";
            this.BuildPackageAction.ImageName = "BuildPackage";
            this.BuildPackageAction.Shortcut = null;
            this.BuildPackageAction.Tag = null;
            this.BuildPackageAction.TargetObjectsCriteria = null;
            this.BuildPackageAction.TargetObjectType = typeof(XafDelta.Messaging.Package);
            this.BuildPackageAction.TargetViewId = null;
            this.BuildPackageAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.BuildPackageAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.BuildPackageAction.ToolTip = "Create new packages based on protocol";
            this.BuildPackageAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.BuildPackageAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.BuildPackage_Execute);
            // 
            // LoadAllAction
            // 
            this.LoadAllAction.Caption = "Load all packages";
            this.LoadAllAction.Category = "RecordEdit";
            this.LoadAllAction.ConfirmationMessage = "Load all pending packets into database ?";
            this.LoadAllAction.Id = "LoadAllAction";
            this.LoadAllAction.ImageName = "LoadAll";
            this.LoadAllAction.Shortcut = null;
            this.LoadAllAction.Tag = null;
            this.LoadAllAction.TargetObjectsCriteria = null;
            this.LoadAllAction.TargetObjectType = typeof(XafDelta.Messaging.Package);
            this.LoadAllAction.TargetViewId = null;
            this.LoadAllAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.LoadAllAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.LoadAllAction.ToolTip = "Playback all pending packets in context of application database";
            this.LoadAllAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.LoadAllAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.LoadAllAction_Execute);
            // 
            // LoadPackageAction
            // 
            this.LoadPackageAction.Caption = "Load package";
            this.LoadPackageAction.Category = "RecordEdit";
            this.LoadPackageAction.ConfirmationMessage = "Load selected packages into database ?";
            this.LoadPackageAction.Id = "LoadPackageAction";
            this.LoadPackageAction.ImageName = "LoadPackage";
            this.LoadPackageAction.Shortcut = null;
            this.LoadPackageAction.Tag = null;
            this.LoadPackageAction.TargetObjectsCriteria = null;
            this.LoadPackageAction.TargetObjectType = typeof(XafDelta.Messaging.Package);
            this.LoadPackageAction.TargetViewId = null;
            this.LoadPackageAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.LoadPackageAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.LoadPackageAction.ToolTip = "Playback selected packages in application database";
            this.LoadPackageAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.LoadPackageAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.LoadPackageAction_Execute);
            // 
            // BuildSnapshotAction
            // 
            this.BuildSnapshotAction.Caption = "Build snapshot";
            this.BuildSnapshotAction.Category = "RecordEdit";
            this.BuildSnapshotAction.ConfirmationMessage = "Create database snapshot for selected replication node ?";
            this.BuildSnapshotAction.Id = "BuildSnapshotAction";
            this.BuildSnapshotAction.ImageName = "BuildSnapshot";
            this.BuildSnapshotAction.SelectionDependencyType = DevExpress.ExpressApp.Actions.SelectionDependencyType.RequireSingleObject;
            this.BuildSnapshotAction.Shortcut = null;
            this.BuildSnapshotAction.Tag = null;
            this.BuildSnapshotAction.TargetObjectsCriteria = null;
            this.BuildSnapshotAction.TargetObjectType = typeof(XafDelta.ReplicationNode);
            this.BuildSnapshotAction.TargetViewId = null;
            this.BuildSnapshotAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.BuildSnapshotAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.BuildSnapshotAction.ToolTip = "Create database snapshot for selected replication node";
            this.BuildSnapshotAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.BuildSnapshotAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.BuildSnapshotAction_Execute);
            // 
            // ExportFilesAction
            // 
            this.ExportFilesAction.Caption = "Export";
            this.ExportFilesAction.Category = "RecordEdit";
            this.ExportFilesAction.ConfirmationMessage = null;
            this.ExportFilesAction.Id = "ExportFilesAction";
            this.ExportFilesAction.ImageName = "ExportFiles";
            this.ExportFilesAction.Shortcut = null;
            this.ExportFilesAction.Tag = null;
            this.ExportFilesAction.TargetObjectsCriteria = null;
            this.ExportFilesAction.TargetViewId = null;
            this.ExportFilesAction.ToolTip = "Save selected items to files ";
            this.ExportFilesAction.TypeOfView = null;
            this.ExportFilesAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.ExportFilesAction_Execute);
            // 
            // ImportFilesAction
            // 
            this.ImportFilesAction.AcceptButtonCaption = null;
            this.ImportFilesAction.CancelButtonCaption = null;
            this.ImportFilesAction.Caption = "Import";
            this.ImportFilesAction.Category = "RecordEdit";
            this.ImportFilesAction.ConfirmationMessage = null;
            this.ImportFilesAction.Id = "ImportFilesAction";
            this.ImportFilesAction.ImageName = "ImportFiles";
            this.ImportFilesAction.Shortcut = null;
            this.ImportFilesAction.Tag = null;
            this.ImportFilesAction.TargetObjectsCriteria = null;
            this.ImportFilesAction.TargetViewId = null;
            this.ImportFilesAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.ImportFilesAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.ImportFilesAction.ToolTip = "Load data from files";
            this.ImportFilesAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.ImportFilesAction.CustomizePopupWindowParams += new DevExpress.ExpressApp.Actions.CustomizePopupWindowParamsEventHandler(this.ImportFilesAction_CustomizePopupWindowParams);
            this.ImportFilesAction.Execute += new DevExpress.ExpressApp.Actions.PopupWindowShowActionExecuteEventHandler(this.ImportFilesAction_Execute);
            // 
            // WinImportFilesAction
            // 
            this.WinImportFilesAction.Caption = "Import";
            this.WinImportFilesAction.Category = "RecordEdit";
            this.WinImportFilesAction.ConfirmationMessage = null;
            this.WinImportFilesAction.Id = "WinImportFilesAction";
            this.WinImportFilesAction.ImageName = "ImportFiles";
            this.WinImportFilesAction.Shortcut = null;
            this.WinImportFilesAction.Tag = null;
            this.WinImportFilesAction.TargetObjectsCriteria = null;
            this.WinImportFilesAction.TargetViewId = null;
            this.WinImportFilesAction.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.WinImportFilesAction.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.WinImportFilesAction.ToolTip = "Load data from files";
            this.WinImportFilesAction.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.WinImportFilesAction.Execute += new DevExpress.ExpressApp.Actions.SimpleActionExecuteEventHandler(this.WinImportFilesAction_Execute);
            // 
            // BuildSnapshotActionPop
            // 
            this.BuildSnapshotActionPop.AcceptButtonCaption = null;
            this.BuildSnapshotActionPop.CancelButtonCaption = null;
            this.BuildSnapshotActionPop.Caption = "Build snapshot";
            this.BuildSnapshotActionPop.Category = "RecordEdit";
            this.BuildSnapshotActionPop.ConfirmationMessage = null;
            this.BuildSnapshotActionPop.Id = "BuildSnapshotActionPop";
            this.BuildSnapshotActionPop.ImageName = "BuildSnapshot";
            this.BuildSnapshotActionPop.Shortcut = null;
            this.BuildSnapshotActionPop.Tag = null;
            this.BuildSnapshotActionPop.TargetObjectsCriteria = null;
            this.BuildSnapshotActionPop.TargetObjectType = typeof(XafDelta.Messaging.Package);
            this.BuildSnapshotActionPop.TargetViewId = null;
            this.BuildSnapshotActionPop.TargetViewNesting = DevExpress.ExpressApp.Nesting.Root;
            this.BuildSnapshotActionPop.TargetViewType = DevExpress.ExpressApp.ViewType.ListView;
            this.BuildSnapshotActionPop.ToolTip = "Create database snapshot for replication node";
            this.BuildSnapshotActionPop.TypeOfView = typeof(DevExpress.ExpressApp.ListView);
            this.BuildSnapshotActionPop.CustomizePopupWindowParams += new DevExpress.ExpressApp.Actions.CustomizePopupWindowParamsEventHandler(this.BuildSnapshotActionPop_CustomizePopupWindowParams);
            this.BuildSnapshotActionPop.Execute += new DevExpress.ExpressApp.Actions.PopupWindowShowActionExecuteEventHandler(this.BuildSnapshotActionPop_Execute);
            // 
            // VcActions
            // 
            this.Activated += new System.EventHandler(this.VcActions_Activated);

        }

        #endregion

        /// <summary>
        /// ExportFilesAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.SimpleAction ExportFilesAction;
        /// <summary>
        /// BuildPackageAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.SimpleAction BuildPackageAction;
        /// <summary>
        /// LoadAllAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.SimpleAction LoadAllAction;
        /// <summary>
        /// LoadPackageAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.SimpleAction LoadPackageAction;
        /// <summary>
        /// BuildSnapshotAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.SimpleAction BuildSnapshotAction;
        /// <summary>
        /// ImportFilesAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.PopupWindowShowAction ImportFilesAction;
        /// <summary>
        /// WinImportFilesAction
        /// </summary>
        public DevExpress.ExpressApp.Actions.SimpleAction WinImportFilesAction;
        private DevExpress.ExpressApp.Actions.PopupWindowShowAction BuildSnapshotActionPop;

    }
}

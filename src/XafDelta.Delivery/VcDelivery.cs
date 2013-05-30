using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace XafDelta.Delivery
{
    public partial class VcDelivery : ViewController
    {
        public VcDelivery()
        {
            InitializeComponent();
            RegisterActions(components);
        }

        public IXafDeltaPlatform GetPlatform(string actionName)
        {
            var args = new GetPlatformArgs();
            XafDeltaModule.Instance.OnGetPlatform(args);
            if (args.XafDeltaPlatform == null) throw new ApplicationException("XafDeltaPlatform is null");
            if (args.XafDeltaPlatform.ActionIsBusy(actionName)) args.XafDeltaPlatform = null;
            return args.XafDeltaPlatform;
        }

        private void actionComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            View.ObjectSpace.Refresh();
        }

        private void DownloadMessagesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
                platform.ExecuteAction(e.Action.Id, downloadMessagesExec, new ActionWorker(), actionComplete);
        }

        private void downloadMessagesExec(object sender, DoWorkEventArgs e)
        {
            var worker = (ActionWorker)e.Argument;
            using (var objectSpace = DeliveryModule.Instance.Application.CreateObjectSpace())
            {
                DeliveryModule.Instance.DeliveryService.Download(XafDeltaModule.Instance, objectSpace, worker);
                objectSpace.CommitChanges();
            }
        }

        private void UploadMessagesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
                platform.ExecuteAction(e.Action.Id, uploadMessagesExec, new ActionWorker(), actionComplete);
        }

        private void uploadMessagesExec(object sender, DoWorkEventArgs e)
        {
            var worker = (ActionWorker)e.Argument;
            using (var objectSpace = DeliveryModule.Instance.Application.CreateObjectSpace())
            {
                DeliveryModule.Instance.DeliveryService.Upload(XafDeltaModule.Instance, objectSpace, worker);
                objectSpace.CommitChanges();
            }
        }
    }
}

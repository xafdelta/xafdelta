using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Web.SystemModule;

namespace XafDelta.Web
{
    public partial class VcWebActions : ViewController
    {
        public VcWebActions()
        {
            InitializeComponent();
            RegisterActions(components);
        }

        #region Changes

        private WebDetailViewController detController;

        protected override void OnActivated()
        {
            base.OnActivated();

            detController = Frame.GetController<WebDetailViewController>();

            detController.EditAction.Executing += collectObjectSpace;
            detController.SaveAction.Executing += collectObjectSpace;
            detController.SaveAndCloseAction.Executing += collectObjectSpace;
            detController.SaveAndNewAction.Executing += collectObjectSpace;

            detController.EditAction.Execute += collectExecute;
            detController.SaveAction.Execute += collectExecute;
            detController.SaveAndCloseAction.Execute += collectExecute;
            detController.SaveAndNewAction.Execute += collectExecute;
        }

        protected override void OnDeactivated()
        {
            if (detController != null)
            {
                detController.EditAction.Executing -= collectObjectSpace;
                detController.SaveAction.Executing -= collectObjectSpace;
                detController.SaveAndCloseAction.Executing -= collectObjectSpace;
                detController.SaveAndNewAction.Executing -= collectObjectSpace;

                detController.EditAction.Execute -= collectExecute;
                detController.SaveAction.Execute -= collectExecute;
                detController.SaveAndCloseAction.Execute -= collectExecute;
                detController.SaveAndNewAction.Execute -= collectExecute;
            }
            base.OnDeactivated();
        }

        void collectExecute(object sender, SimpleActionExecuteEventArgs e)
        {
            if (detController != null && detController.View != null)
            {
                XafDeltaModule.Instance.CollectObjectSpace(detController.View.ObjectSpace);
            }
        }

        void collectObjectSpace(object sender, CancelEventArgs e)
        {
            if (detController != null && detController.View != null)
            {
                XafDeltaModule.Instance.CollectObjectSpace(detController.View.ObjectSpace);
            }
        }

        #endregion
    }
}

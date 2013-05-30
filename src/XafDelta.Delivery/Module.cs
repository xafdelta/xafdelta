using System;
using System.ComponentModel;
using System.Drawing;
using DevExpress.ExpressApp;
using DevExpress.Utils;
using XafDelta.Messaging;

namespace XafDelta.Delivery
{
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [Description("XafDelta message delivery system module.")]
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxBitmap(typeof(DeliveryModule), "Resources.Delta.ico")]
    public sealed partial class DeliveryModule : ModuleBase
    {
        public DeliveryModule()
        {
            ModelDifferenceResourceName = "XafDelta.Delivery.Model.DesignedDiffs";
            InitializeComponent();
            Instance = this;
            DeliveryService = new DeliveryService(this);
            ResourcesExportedToModel.Add(typeof(Localization.Localizer));
        }

        public static DeliveryModule Instance { get; private set; }

        public DeliveryService DeliveryService { get; private set; }

        public event EventHandler<SelectUploadTransportArgs> SelectUploadTransport;
        internal void OnSelectUploadTransport(SelectUploadTransportArgs e)
        {
            var handler = SelectUploadTransport;
            if (handler != null) handler(this, e);
        }
    }

    public class SelectUploadTransportArgs : EventArgs
    {
        public IReplicationMessage Message { get; private set; }
        public IXafDeltaTransport Transport { get; private set; }

        public SelectUploadTransportArgs(IReplicationMessage message, IXafDeltaTransport transport)
        {
            Message = message;
            Transport = transport;
        }
    }
}

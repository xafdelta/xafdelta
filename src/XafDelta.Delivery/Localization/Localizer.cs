using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Utils;

namespace XafDelta.Delivery.Localization
{
    public class Localizer : XafResourceLocalizer
    {
        public static readonly string LocalizationGroup = "ReplicationDelivery";

        internal static string DownloadStarted { get { return localStr("DownloadStarted"); } }
        internal static string TransportFound { get { return localStr("TransportFound"); } }
        internal static string DownloadUsing { get { return localStr("DownloadUsing"); } }
        internal static string OpenTransport { get { return localStr("OpenTransport"); } }
        internal static string FilesForDownload { get { return localStr("FilesForDownload"); } }
        internal static string DownloadFile { get { return localStr("DownloadFile"); } }
        internal static string DownloadError { get { return localStr("DownloadError"); } }
        internal static string CloseTransport { get { return localStr("CloseTransport"); } }
        internal static string DownloadFinished { get { return localStr("DownloadFinished"); } }
        internal static string UploadStarted { get { return localStr("UploadStarted"); } }
        internal static string FoundForUpload { get { return localStr("FoundForUpload"); } }
        internal static string UploadingFile { get { return localStr("UploadingFile"); } }
        internal static string UploadError { get { return localStr("UploadError"); } }
        internal static string UploadTransportNotFound { get { return localStr("UploadTransportNotFound"); } }
        internal static string UploadFinished { get { return localStr("UploadFinished"); } }

        protected override IXafResourceManagerParameters GetXafResourceManagerParameters()
        {
            var localizationGroupPath = new[] { LocalizationGroup };
            return new XafResourceManagerParameters(localizationGroupPath,
                                                    "XafDelta.Delivery.Localization.LocalizationStrings", "", GetType().Assembly);
        }

        private static string localStr(string resourceName)
        {
            return CaptionHelper.GetLocalizedText(LocalizationGroup, resourceName);
        }
    }
}
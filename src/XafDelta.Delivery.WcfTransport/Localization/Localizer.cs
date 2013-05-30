using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Utils;

namespace XafDelta.Delivery.WcfTransport.Localization
{
    public class Localizer : XafResourceLocalizer
    {
        public static readonly string LocalizationGroup = "WcfTransport";

        internal static string ClientIsNotOpened { get { return LocalStr("ClientIsNotOpened"); } }
        internal static string FileListingOk { get { return LocalStr("FileListingOk"); } }
        internal static string ClientOpened { get { return LocalStr("ClientOpened"); } }
        internal static string FileListingError { get { return LocalStr("FileListingError"); } }
        internal static string DownloadOk { get { return LocalStr("DownloadOk"); } }
        internal static string DownloadError { get { return LocalStr("DownloadError"); } }
        internal static string UploadOk { get { return LocalStr("UploadOk"); } }
        internal static string UploadError { get { return LocalStr("UploadError"); } }


        protected override IXafResourceManagerParameters GetXafResourceManagerParameters()
        {
            var localizationGroupPath = new[] { LocalizationGroup };
            return new XafResourceManagerParameters(localizationGroupPath,
                                                    "XafDelta.Delivery.WcfTransport.Localization.LocalizationStrings", "", GetType().Assembly);
        }

        public static string LocalStr(string resourceName)
        {
            return CaptionHelper.GetLocalizedText(LocalizationGroup, resourceName);
        }
    }
}
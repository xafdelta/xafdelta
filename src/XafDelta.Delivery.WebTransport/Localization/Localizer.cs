using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Utils;

namespace XafDelta.Delivery.WebTransport.Localization
{
    public class Localizer : XafResourceLocalizer
    {
        public static readonly string LocalizationGroup = "WebTransport";

        internal static string Listing { get { return localStr("Listing"); } }
        internal static string Listed { get { return localStr("Listed"); } }
        internal static string Downloading { get { return localStr("Downloading"); } }
        internal static string Downloaded { get { return localStr("Downloaded"); } }
        internal static string Uploading { get { return localStr("Uploading"); } }
        internal static string Uploaded { get { return localStr("Uploaded"); } }
        internal static string Deleting { get { return localStr("Deleting"); } }
        internal static string Deleted { get { return localStr("Deleted"); } }
        internal static string CommandError { get { return localStr("CommandError"); } }
        internal static string InvalidUri { get { return localStr("InvalidUri"); } }

        protected override IXafResourceManagerParameters GetXafResourceManagerParameters()
        {
            var localizationGroupPath = new[] { LocalizationGroup };
            return new XafResourceManagerParameters(localizationGroupPath,
                                                    "XafDelta.Delivery.WebTransport.Localization.LocalizationStrings", "", GetType().Assembly);
        }

        private static string localStr(string resourceName)
        {
            return CaptionHelper.GetLocalizedText(LocalizationGroup, resourceName);
        }
    }
}
using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Utils;

namespace XafDelta.Localization
{
    /// <summary>
    /// Xaf delta resource localizer. For internal use only.
    /// </summary>
    public class Localizer : XafResourceLocalizer
    {
        internal static readonly string LocalizationGroup = "Replication";

        internal static string NotSessionProvider { get { return localStr("NotSessionProvider"); } }
        internal static string CantDeleteNode { get { return localStr("CantDeleteNode"); } }
        internal static string CantCreateSnapshotForDisabled { get { return localStr("CantCreateSnapshotForDisabled"); } }
        internal static string NodeIdInvalidChars { get { return localStr("NodeIdInvalidChars"); } }
        internal static string InvalidPackageData { get { return localStr("InvalidPackageData"); } }
        internal static string NodeIdAllNodes { get { return localStr("NodeIdAllNodes"); } }
        internal static string LoadingStarted { get { return localStr("LoadingStarted"); } }
        internal static string FilesSelectedForImport { get { return localStr("FilesSelectedForImport"); } }
        internal static string ImportingFile { get { return localStr("ImportingFile"); } }
        internal static string ImportPackageExists { get { return localStr("ImportPackageExists"); } }
        internal static string ImportTicketExists { get { return localStr("ImportTicketExists"); } }
        internal static string ImportInvalidFileExtension { get { return localStr("ImportInvalidFileExtension"); } }
        internal static string ImportAborted { get { return localStr("ImportAborted"); } }
        internal static string FilesSelectedForExport { get { return localStr("FilesSelectedForExport"); } }
        internal static string ExportingFile { get { return localStr("ExportingFile"); } }
        internal static string ExportAborted { get { return localStr("ExportAborted"); } }
        internal static string ExportFinished { get { return localStr("ExportFinished"); } }
        internal static string SelectedForLoading { get { return localStr("SelectedForLoading"); } }
        internal static string LoadingAborted { get { return localStr("LoadingAborted"); } }
        internal static string ShouldSaveTargetNode { get { return localStr("ShouldSaveTargetNode"); } }
        internal static string PackagesSelectedForLoading { get { return localStr("PackagesSelectedForLoading"); } }
        internal static string LoadingIsFinished { get { return localStr("LoadingIsFinished"); } }
        internal static string LoadingPackage { get { return localStr("LoadingPackage"); } }
        internal static string PackageLoadingIsFailed { get { return localStr("PackageLoadingIsFailed"); } }
        internal static string PackageLoadingCompleted { get { return localStr("PackageLoadingCompleted"); } }
        internal static string PackageRejected { get { return localStr("PackageRejected"); } }
        internal static string SenderNodeIsNotFound { get { return localStr("SenderNodeIsNotFound"); } }
        internal static string InvalidPackageId { get { return localStr("InvalidPackageId"); } }
        internal static string PackageDataIsEmpty { get { return localStr("PackageDataIsEmpty"); } }
        internal static string CollisionError { get { return localStr("CollisionError"); } }
        internal static string TargetObjectExists { get { return localStr("TargetObjectExists"); } }
        internal static string TargetObjectNotFound { get { return localStr("TargetObjectNotFound"); } }
        internal static string FileNameIsEmpty { get { return localStr("FileNameIsEmpty"); } }
        internal static string InvalidPackageFileName { get { return localStr("InvalidPackageFileName"); } }
        internal static string InvalidPackageProp { get { return localStr("InvalidPackageProp"); } }
        internal static string ObjectsFoundInSnapshot { get { return localStr("ObjectsFoundInSnapshot"); } }
        internal static string SnapshotLoadingIsFailed { get { return localStr("SnapshotLoadingIsFailed"); } }
        internal static string SnapshotLoadingIs { get { return localStr("SnapshotLoadingIs"); } }
        internal static string Aborted { get { return localStr("Aborted"); } }
        internal static string Finished { get { return localStr("Finished"); } }
        internal static string BuildingSnapshotForNode { get { return localStr("BuildingSnapshotForNode"); } }
        internal static string TotalObjectsSnapshoted { get { return localStr("TotalObjectsSnapshoted"); } }
        internal static string NoObjectsFoundForSnapshot { get { return localStr("NoObjectsFoundForSnapshot"); } }
        internal static string SnapshotFailed { get { return localStr("SnapshotFailed"); } }
        internal static string SnapshotBuildingIs { get { return localStr("SnapshotBuildingIs"); } }
        internal static string SourceObjectsFoundForClass { get { return localStr("SourceObjectsFoundForClass"); } }
        internal static string PackageLoadError { get { return localStr("PackageLoadError"); } }
        internal static string LoadingIs { get { return localStr("LoadingIs"); } }
        internal static string SessionAlreadyLoaded { get { return localStr("SessionAlreadyLoaded"); } }
        internal static string CollisionDetected { get { return localStr("CollisionDetected"); } }
        internal static string BuildingPackages { get { return localStr("BuildingPackages"); } }
        internal static string PackageSaveFailed { get { return localStr("PackageSaveFailed"); } }
        internal static string PackageSavingIs { get { return localStr("PackageSavingIs"); } }
        internal static string SavingSessionData { get { return localStr("SavingSessionData"); } }
        internal static string SessionDataSaved { get { return localStr("SessionDataSaved"); } }
        internal static string PackageCreated { get { return localStr("PackageCreated"); } }
        internal static string ImportFinished { get { return localStr("ImportFinished"); } }
        internal static string BuildObjectMaps { get { return localStr("BuildObjectMaps"); } }
        internal static string CommitChanges { get { return localStr("CommitChanges"); } }
        internal static string SnapshotProperty { get { return localStr("SnapshotProperty"); } }
        internal static string SnapshotObject { get { return localStr("SnapshotObject"); } }
        

        /// <summary>
        /// Gets the xaf resource manager parameters.
        /// </summary>
        /// <returns>Xaf resource manager parameters</returns>
        protected override IXafResourceManagerParameters GetXafResourceManagerParameters()
        {
            var localizationGroupPath = new[] { LocalizationGroup };
            return new XafResourceManagerParameters(localizationGroupPath,
                                                    "XafDelta.Localization.LocalizationStrings", "", GetType().Assembly);
        }

        private static string localStr(string resourceName)
        {
            return CaptionHelper.GetLocalizedText(LocalizationGroup, resourceName);
        }

    }
}
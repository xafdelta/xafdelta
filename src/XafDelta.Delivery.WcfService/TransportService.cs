using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace XafDelta.Delivery.WcfService
{
    /// <summary>
    /// Transport service
    /// </summary>
    public class TransportService : ITransportService
    {
        #region Implementation of ITransportService

        public IEnumerable<string> GetFileNames(string mask)
        {
            var dirIndo = new DirectoryInfo(getFilesPath());
            return dirIndo.GetFiles().Select(x => x.Name)
                .Where(fileName => Regex.IsMatch(fileName, mask)).ToList();
        }

        private string getFilesPath()
        {
            var result = Properties.Settings.Default.filesPath;
            if (string.IsNullOrEmpty(result))
                result = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                         + Path.DirectorySeparatorChar + "Files";
            if (!Directory.Exists(result))
                Directory.CreateDirectory(result);
            (new FileIOPermission(FileIOPermissionAccess.AllAccess, result)).Demand();
            return result;
        }

        public void UploadFile(string fileName, byte[] fileData)
        {
            var fileFullName = Path.Combine(getFilesPath(), fileName);
            File.WriteAllBytes(fileFullName, fileData);

        }

        public byte[] DownloadFile(string fileName)
        {
            var fileFullName = Path.Combine(getFilesPath(), fileName);
            (new FileIOPermission(FileIOPermissionAccess.Read, fileFullName)).Demand();
            return File.ReadAllBytes(fileFullName);
        }

        public void DeleteFile(string fileName)
        {
            var fileFullName = Path.Combine(getFilesPath(), fileName);
            (new FileIOPermission(FileIOPermissionAccess.Read, fileFullName)).Demand();
            if(File.Exists(fileFullName))
                File.Delete(fileFullName);
        }

        #endregion
    }
}

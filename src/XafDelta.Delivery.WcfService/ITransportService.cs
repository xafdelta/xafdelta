using System.Collections.Generic;
using System.ServiceModel;

namespace XafDelta.Delivery.WcfService
{
    /// <summary>
    /// Transport service interface
    /// </summary>
    [ServiceContract]
    public interface ITransportService
    {
        [OperationContract]
        IEnumerable<string> GetFileNames(string mask);

        [OperationContract]
        void UploadFile(string fileName, byte[] fileData);

        [OperationContract]
        byte[] DownloadFile(string fileName);

        [OperationContract]
        void DeleteFile(string fileName);
    }
}

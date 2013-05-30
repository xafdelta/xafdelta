using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using XafDelta.Messaging;
using XafDelta.Delivery.Localization;

namespace XafDelta.Delivery
{
    /// <summary>
    /// Message delivery service
    /// </summary>
    public sealed class DeliveryService
    {
        /// <summary>
        /// Gets the owner.
        /// </summary>
        public DeliveryModule Owner { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryService"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public DeliveryService(DeliveryModule owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Downloads XafDelta messages into replication storage database.
        /// </summary>
        /// <param name="xafDeltaModule">The xaf delta module.</param>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="worker">The worker.</param>
        public void Download(XafDeltaModule xafDeltaModule, IObjectSpace objectSpace, ActionWorker worker)
        {
            worker.ReportProgress(Localizer.DownloadStarted);

            var transportList = (from m in xafDeltaModule.ModuleManager.Modules
                                where m is IXafDeltaTransport && ((IXafDeltaTransport) m).UseForDownload
                                select m).Cast<IXafDeltaTransport>().ToList();

            worker.ReportProgress(string.Format(Localizer.TransportFound, transportList.Count()));
            foreach (var transport in transportList.TakeWhile(x => !worker.CancellationPending))
            {
                worker.ReportProgress(string.Format(Localizer.DownloadUsing, transport));
                try
                {
                    worker.ReportProgress(string.Format(Localizer.OpenTransport, transport));
                    transport.Open(TransportMode.Download, worker);

                    var existingReplicaNames = from c in objectSpace.GetObjects<Package>() select c.FileName;

                    var fileNames = transport.GetFileNames(worker,
                        @"(" + Ticket.FileMask + "|" + Package.FileMask + ")").ToList();

                    fileNames = fileNames.Except(existingReplicaNames).ToList();

                    worker.ReportProgress(string.Format(Localizer.FilesForDownload, fileNames.Count));
                    foreach (var fileName in fileNames.TakeWhile(x => !worker.CancellationPending))
                    {
                        // worker.ReportProgress(string.Format(Localizer.DownloadFile, fileName));
                        var fileData = transport.DownloadFile(fileName, worker);
                        if(fileData != null && fileData.Length > 0)
                        {
                            if (fileName.EndsWith(Package.PackageFileExtension))
                            {
                                var replica = Package.ImportFromBytes(objectSpace, fileName, fileData);
                                replica.Save();
                            }
                            else
                            {
                                var replicaTicket = Ticket.ImportTicket(objectSpace, fileData);
                                replicaTicket.Save();
                            }
                            if (!fileName.Contains(ReplicationNode.AllNodes))
                                transport.DeleteFile(fileName, worker);
                            objectSpace.CommitChanges();
                        }
                    }
                }
                catch (Exception exception)
                {
                    objectSpace.Rollback();
                    worker.ReportError(Localizer.DownloadError, exception.Message);
                }
                finally
                {
                    worker.ReportProgress(Localizer.CloseTransport, transport);
                    transport.Close();
                }
            }
            worker.ReportProgress(Color.Blue, Localizer.DownloadFinished);
        }

        /// <summary>
        /// Uploads pending XafDelta messages to intermidiate net storages.
        /// </summary>
        /// <param name="xafDeltaModule">The xaf delta module.</param>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="worker">The worker.</param>
        public void Upload(XafDeltaModule xafDeltaModule, IObjectSpace objectSpace, ActionWorker worker)
        {
            worker.ReportProgress(Localizer.UploadStarted);

            var transportList = (from m in XafDeltaModule.XafApp.Modules
                                 where m is IXafDeltaTransport && ((IXafDeltaTransport)m).UseForUpload
                                 select m).Cast<IXafDeltaTransport>().ToList().AsReadOnly();

            if (transportList.Count > 0)
            {
                var replicas = (from r in objectSpace.GetObjects<Package>() where r.IsOutput 
                                    && r.GetEventDateTime(PackageEventType.Sent) == DateTime.MinValue
                                    orderby r.PackageDateTime
                                select r).ToList();

                var tickets = (from t in objectSpace.GetObjects<Ticket>(CriteriaOperator.Parse("IsNull(ProcessingDateTime)"), true)
                               where t.Package != null && t.Package.IsInput
                               orderby t.TicketDateTime select t).ToList();

                var messages = replicas.Cast<IReplicationMessage>().Union(tickets.Cast<IReplicationMessage>()).ToList();

                var uploadData = new Dictionary<IXafDeltaTransport, List<IReplicationMessage>>();

                worker.ReportProgress(string.Format(Localizer.FoundForUpload, messages.Count()));
                foreach (var message in messages.TakeWhile(x => !worker.CancellationPending))
                {
                    var args = new SelectUploadTransportArgs(message, transportList[0]);
                    Owner.OnSelectUploadTransport(args);
                    if(args.Transport != null)
                    {
                        List<IReplicationMessage> list;
                        if(!uploadData.TryGetValue(args.Transport, out list))
                        {
                            list = new List<IReplicationMessage>();
                            uploadData.Add(args.Transport, list);
                        }
                        list.Add(message);
                    }
                }

                if(!worker.CancellationPending && uploadData.Keys.Count > 0)
                {
                    foreach (var transport in uploadData.Keys.TakeWhile(x => !worker.CancellationPending))
                    {
                        try
                        {
                            worker.ReportProgress(string.Format(Localizer.OpenTransport, transport));
                            transport.Open(TransportMode.Upload, worker);
                            var messageList = uploadData[transport];
                            foreach (var message in messageList)
                            {
                                var recipientAddress = message.RecipientAddress;
                                // worker.ReportProgress(string.Format(Localizer.UploadingFile, message));
                                transport.UploadFile(message.ToString(), message.GetData(), recipientAddress, worker);
                                if (message is Package)
                                    ((Package)message).CreateLogRecord(PackageEventType.Sent);
                                else
                                    ((Ticket) message).ProcessingDateTime = DateTime.Now;

                                objectSpace.CommitChanges();
                            }
                        }
                        catch (Exception exception)
                        {
                            objectSpace.Rollback();
                            worker.ReportError(Localizer.UploadError, exception.Message);
                        }
                        finally
                        {
                            worker.ReportProgress(string.Format(Localizer.CloseTransport, transport));
                            transport.Close();
                        }
                    }
                }
            }
            else
                worker.ReportProgress(Color.BlueViolet, Localizer.UploadTransportNotFound);
            worker.ReportProgress(Color.Blue, Localizer.UploadFinished);
        }
    }
}

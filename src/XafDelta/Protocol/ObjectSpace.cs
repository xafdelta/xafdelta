using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;

namespace XafDelta.Protocol
{
    /// <summary>
    /// XafDelta object space. Implements registration in collector.
    /// </summary>
    public class ObjectSpace: DevExpress.ExpressApp.ObjectSpace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSpace"/> class.
        /// Registers new object space in collector.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="typesInfo">The types info.</param>
        public ObjectSpace(UnitOfWork unitOfWork, ITypesInfo typesInfo) : base(unitOfWork, typesInfo)
        {
            XafDeltaModule.Instance.ProtocolService.Collector.RegisterObjectSpace(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSpace"/> class.
        /// Registers new object space in collector.
        /// </summary>
        /// <param name="unitOfWork">A <b>UnitOfWork</b> object that will be used to load and save persistent objects.</param>
        public ObjectSpace(UnitOfWork unitOfWork) : base(unitOfWork)
        {
            XafDeltaModule.Instance.ProtocolService.Collector.RegisterObjectSpace(this);
        }

        /// <summary>
        /// Creates a nested Object Space of XafDelta.Protocol.Native.NestedObjectSpace type.
        /// </summary>
        /// <returns>
        /// An <see cref="T:DevExpress.ExpressApp.IObjectSpace"/> object that represents a created nested Object Space.
        /// </returns>
        public override IObjectSpace CreateNestedObjectSpace()
        {
            CheckIsDisposed();
            var nestedObjectSpace = new NestedObjectSpace(this)
                                        {
                                            AsyncServerModeSourceResolveSession =
                                                base.AsyncServerModeSourceResolveSession,
                                            AsyncServerModeSourceDismissSession =
                                                base.AsyncServerModeSourceDismissSession
                                        };
            return nestedObjectSpace;
        }

        /// <summary>
        /// Creates an object of the specified type. 
        /// Registers object creation in collector.
        /// </summary>
        /// <param name="type">A <see cref="T:System.Type"/> object which represents the type of the object to be created.</param>
        /// <returns>
        /// An object that represents the created object of the specified type.
        /// </returns>
        public override object CreateObject(Type type)
        {
            var createDateTime = DateTime.UtcNow;
            var eventId = ProtocolEvent.GetNextId();
            var result = base.CreateObject(type);
            XafDeltaModule.Instance.ProtocolService.Collector.RegisterObjectCreation(result, createDateTime, eventId);
            return result;
        }

        new internal Action<ResolveSessionEventArgs> AsyncServerModeSourceDismissSession { set { base.AsyncServerModeSourceDismissSession = value; } }
        new internal Action<ResolveSessionEventArgs> AsyncServerModeSourceResolveSession { set { base.AsyncServerModeSourceResolveSession = value; } }
    }

    /// <summary>
    /// XafDelta nested object space. Registration in collector added.
    /// </summary>
    public class NestedObjectSpace : DevExpress.ExpressApp.NestedObjectSpace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NestedObjectSpace"/> class.
        /// Registers new object space in collector.
        /// </summary>
        /// <param name="parentObjectSpace">The parent object space.</param>
        public NestedObjectSpace(IObjectSpace parentObjectSpace) : base(parentObjectSpace)
        {
            XafDeltaModule.Instance.ProtocolService.Collector.RegisterObjectSpace(this);
        }

        new internal Action<ResolveSessionEventArgs> AsyncServerModeSourceDismissSession { set { base.AsyncServerModeSourceDismissSession = value; } }
        new internal Action<ResolveSessionEventArgs> AsyncServerModeSourceResolveSession { set { base.AsyncServerModeSourceResolveSession = value; } }

        /// <summary>
        /// Creates a nested Object Space of XafDelta.Protocol.Native.NestedObjectSpace type.
        /// </summary>
        /// <returns>
        /// An <see cref="T:DevExpress.ExpressApp.IObjectSpace"/> object that represents a created nested Object Space.
        /// </returns>
        public override IObjectSpace CreateNestedObjectSpace()
        {
            CheckIsDisposed();
            var nestedObjectSpace = new NestedObjectSpace(this)
                                        {
                                            AsyncServerModeSourceResolveSession =
                                                base.AsyncServerModeSourceResolveSession,
                                            AsyncServerModeSourceDismissSession =
                                                base.AsyncServerModeSourceDismissSession
                                        };
            return nestedObjectSpace;
        }

        /// <summary>
        /// Creates an object of the specified type.
        /// Registers object creation in collector.
        /// </summary>
        /// <param name="type">A <see cref="T:System.Type"/> object which represents the type of the object to be created.</param>
        /// <returns>
        /// An object that represents the created object of the specified type.
        /// </returns>
        public override object CreateObject(Type type)
        {
            var createDateTime = DateTime.UtcNow;
            var eventId = ProtocolEvent.GetNextId();
            var result = base.CreateObject(type);
            XafDeltaModule.Instance.ProtocolService.Collector.RegisterObjectCreation(result, createDateTime, eventId);
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using XafDelta.Localization;
using ObjectSpaceProvider = XafDelta.Protocol.ObjectSpaceProvider;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Protocol data collector
    /// </summary>
    internal sealed class Collector
    {
        public Collector(ProtocolService protocolService)
        {
            ProtocolService = protocolService;
        }

        /// <summary>
        /// Gets the owner protocol service.
        /// </summary>
        public ProtocolService ProtocolService { get; private set; }

        /// <summary>
        /// Session registry. Shared through all xaf application instances. Must be static.
        /// </summary>
        private static readonly Dictionary<Session, SessionData> sessionRegistry = new Dictionary<Session, SessionData>();

        private IDataStoreProviderFactory providerFactory;
        

        /// <summary>
        /// Initialize collector.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="dataStoreProviderFactory">The provider factory.</param>
        internal void Initialize(XafApplication application, IDataStoreProviderFactory dataStoreProviderFactory)
        {
            providerFactory = dataStoreProviderFactory;
            application.CreateCustomObjectSpaceProvider += application_CreateCustomObjectSpaceProvider;
            application.ObjectSpaceCreated += application_ObjectSpaceCreated;
            application.SetupComplete += application_SetupComplete;
            application.Disposed += application_Disposed;
        }

        /// <summary>
        /// Handles the SetupComplete event of the application control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void application_SetupComplete(object sender, EventArgs e)
        {
            var application = (XafApplication) sender;
            gatherNotForProtocolItems(application);
        }

        private static readonly HashSet<string> notForProtocolItems = new HashSet<string>();

        /// <summary>
        /// Gathers "not for protocol" items.
        /// </summary>
        /// <param name="application">The application.</param>
        private void gatherNotForProtocolItems(XafApplication application)
        {
            var classes = (from t in application.Model.BOModel select t).ToList();

            foreach (var classType in classes)
            {
                var className = classType.TypeInfo.Type.FullName;
                if (classType.NotForProtocol())
                {
                    if (className != null) notForProtocolItems.Add(className);
                }
                else
                    foreach (var member in classType.OwnMembers)
                        if (member.NotForProtocol())
                            notForProtocolItems.Add(className + "." + member.Name);
            }
            notForProtocolItems.Add("DevExpress.Persistent.BaseImpl.Event.ResourceId");
        }


        /// <summary>
        /// Determines whether the specified class type is not for protocol.
        /// </summary>
        /// <param name="classType">Type of the class.</param>
        /// <returns>
        ///   <c>true</c> if is not for protocol; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsNotForProtocol(Type classType)
        {
            var result = classType.FullName != null && notForProtocolItems.Contains(classType.FullName);
            //if (!result && classType.BaseType != null)
            //    result = IsNotForProtocol(classType.BaseType);
            return result;
        }

        /// <summary>
        /// Determines whether the specified class property is not for protocol.
        /// </summary>
        /// <param name="classType">Type of the class.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>
        ///   <c>true</c> if is not for protocol; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsNotForProtocol(Type classType, string propertyName)
        {
            var result = notForProtocolItems.Contains(classType.FullName + "." + propertyName);
            if (!result && classType.BaseType != null)
                result = IsNotForProtocol(classType.BaseType, propertyName);
            return result;
        }

        /// <summary>
        /// Handles the Disposed event of the application control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void application_Disposed(object sender, EventArgs e)
        {
            var application = (XafApplication) sender;
            application.CreateCustomObjectSpaceProvider -= application_CreateCustomObjectSpaceProvider;
            application.ObjectSpaceCreated -= application_ObjectSpaceCreated;
            application.SetupComplete -= application_SetupComplete;
            application.Disposed -= application_Disposed;
        }

        /// <summary>
        /// Subscribes to session events.
        /// </summary>
        /// <param name="session">The session.</param>
        private void subscribeToSessionEvents(Session session)
        {
            session.Disposed += session_Disposed;
        }

        /// <summary>
        /// Handles the Disposed event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void session_Disposed(object sender, EventArgs e)
        {
            unSubscribeFromSessionEvents((Session) sender);
        }

        /// <summary>
        /// Uns the subscribe from session events.
        /// </summary>
        /// <param name="session">The session.</param>
        private void unSubscribeFromSessionEvents(Session session)
        {
            // remove session from session registry
            if (sessionRegistry.ContainsKey(session))
            {
                sessionRegistry[session].Dispose();
                sessionRegistry.Remove(session);
            }
            session.Disposed -= session_Disposed;
        }

        /// <summary>
        /// Handles the ObjectSpaceCreated event of the application control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.ExpressApp.ObjectSpaceCreatedEventArgs"/> instance containing the event data.</param>
        private void application_ObjectSpaceCreated(object sender, ObjectSpaceCreatedEventArgs e)
        {
            RegisterObjectSpace(e.ObjectSpace);
        }

        /// <summary>
        /// Handles the CreateCustomObjectSpaceProvider event of the application control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.ExpressApp.CreateCustomObjectSpaceProviderEventArgs"/> instance containing the event data.</param>
        private void application_CreateCustomObjectSpaceProvider(object sender, 
            CreateCustomObjectSpaceProviderEventArgs e)
        {
            IXpoDataStoreProvider dataStoreProvider = null;
            if(providerFactory != null)
                dataStoreProvider = providerFactory.CreateDataStoreProvider(e.Connection, e.ConnectionString);
            if (dataStoreProvider == null)
            {
                if (e.Connection == null)
                    dataStoreProvider = new ConnectionStringDataStoreProvider(e.ConnectionString);
                else    
                    dataStoreProvider = new ConnectionDataStoreProvider(e.Connection);
            }
            e.ObjectSpaceProvider = new ObjectSpaceProvider(dataStoreProvider);
        }

        /// <summary>
        /// Registers the session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>New SessionData object</returns>
        internal SessionData RegisterSession(Session session)
        {
            if (collectorDisabled) return null;

            SessionData result;
            if (!sessionRegistry.ContainsKey(session))
            {
                result = new SessionData(session, this);
                sessionRegistry.Add(session, result);
                subscribeToSessionEvents(session);
                if (session is NestedUnitOfWork)
                    result.Parent = RegisterSession(((NestedUnitOfWork)session).Parent);
            }
            else
                result = sessionRegistry[session];

            return result;
        }

        /// <summary>
        /// Registers the object space.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>New SessionData object</returns>
        internal SessionData RegisterObjectSpace(IObjectSpace objectSpace)
        {
            var result = RegisterSession(((DevExpress.ExpressApp.ObjectSpace)objectSpace).Session);
            return result;
        }

        /// <summary>
        /// Collects the object space.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        public void CollectObjectSpace(IObjectSpace objectSpace)
        {
            RegisterObjectSpace(objectSpace);
        }


        /// <summary>
        /// Registers the protocol event.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="protocolEvent">The protocol event.</param>
        internal void RegisterProtocolEvent(Session session, ProtocolEvent protocolEvent)
        {
            if (collectorDisabled) return;

            if (!sessionRegistry.ContainsKey(session))
                RegisterSession(session);
            sessionRegistry[session].RegisterProtocolEvent(protocolEvent);
        }

        /// <summary>
        /// Registers the object creation.
        /// </summary>
        /// <param name="newObject">The new object.</param>
        /// <param name="createDateTime">The create date time.</param>
        /// <param name="eventId">The event id.</param>
        internal void RegisterObjectCreation(object newObject, DateTime createDateTime, long eventId)
        {
            if (collectorDisabled) return;

            if (newObject == null) throw new ArgumentNullException("newObject");
            if (!(newObject is ISessionProvider))
                throw new ArgumentException(Localizer.NotSessionProvider, "newObject");

            var session = ((ISessionProvider) newObject).Session;
            if (!sessionRegistry.ContainsKey(session))
                RegisterSession(session);

            sessionRegistry[session].RegisterObjectCreation(newObject, createDateTime, eventId);
        }

        /// <summary>
        /// Gets a value indicating whether collector is disabled at now.
        /// </summary>
        /// <value>
        ///   <c>true</c> if collector is disabled; otherwise, <c>false</c>.
        /// </value>
        private bool collectorDisabled
        {
            get
            {
                return  SecuritySystem.CurrentUser == null || ProtocolService.Owner.LoadService.IsLoading;
            }
        }

       
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AuditTrail;
using DevExpress.Persistent.AuditTrail;
using DevExpress.Xpo;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Protocol service
    /// </summary>
    internal sealed class ProtocolService: BaseService
    {
        public ProtocolService(XafDeltaModule owner, ILoadService loadService) : base(owner)
        {
            LoadService = loadService;
            Collector = new Collector(this);
        }

        public Collector Collector { get; private set; }

        internal ILoadService LoadService { get; private set; }

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="providerFactory">The provider factory.</param>
        /// <param name="loadService">The load service.</param>
        internal void Initialize(XafApplication application,
            IDataStoreProviderFactory providerFactory, ILoadService loadService)
        {
            LoadService = loadService;
            setupCollector(application, providerFactory);
        }

        /// <summary>
        /// Setups the collector.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="providerFactory">The provider factory.</param>
        private void setupCollector(XafApplication application, IDataStoreProviderFactory providerFactory)
        {
            var auditTrailModule = getAuditTrailModule(application);
            if (auditTrailModule == null)
                Collector.Initialize(application, providerFactory);
        }
        
        /// <summary>
        /// Determines whether method executes in context of specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>
        ///   <c>true</c> if method executes; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSessionInMethod(Session session)
        {
            bool result;
            lock (callRegistry)
            {
                result = callRegistry.ContainsKey(session) && callRegistry[session].ContainsKey(Thread.CurrentThread);
            }
            return result;
        }

        /// <summary>
        /// Method call registry
        /// </summary>
        private readonly Dictionary<Session, Dictionary<Thread, Stack<string>>> callRegistry = 
            new Dictionary<Session, Dictionary<Thread, Stack<string>>>();

        /// <summary>
        /// Enters the method.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="callContext">The call context.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">The args.</param>
        public void EnterMethod(Session session, object callContext, string methodName, params object[] args)
        {
            if (AuditTrailService.Instance.AuditDataStore != null) return;
            if(LoadService.IsLoading) return;
            
            if (session == null) throw new ArgumentNullException("session");
            if (callContext == null) throw new ArgumentNullException("callContext");
            if (methodName == null) throw new ArgumentNullException("methodName");

            var contextObject = callContext is Type ? null : callContext;
            var contextClass = callContext is Type ? (Type)callContext : callContext.GetType();
            var methodInfo = contextClass.GetMethod(methodName);
            if (methodInfo == null) return;

            var replicationKey = "";
            if (contextObject != null)
                replicationKey = ExtensionsHelper.GetReplicationKey(contextObject);

            var protocolEvent = new ProtocolEvent
                                    {
                                        Target = contextObject,
                                        ProtocolEventType = ProtocolEventType.MethodCall,
                                        OldValue = MethodCallParams.CreateForArgs(DevExpress.ExpressApp.ObjectSpace.FindObjectSpaceByObject(contextObject), args),
                                        PropertyName = methodName,
                                        ReplicationKey = replicationKey
                                    };
            Collector.RegisterProtocolEvent(session, protocolEvent);
                                                     
            lock (callRegistry)
            {
                Dictionary<Thread, Stack<string>> sessionThreads;
                if (!callRegistry.TryGetValue(session, out sessionThreads))
                {
                    sessionThreads = new Dictionary<Thread, Stack<string>>();
                    callRegistry.Add(session, sessionThreads);
                }

                var currentThread = Thread.CurrentThread;
                Stack<string> callsStack;
                if (!sessionThreads.TryGetValue(currentThread, out callsStack))
                {
                    callsStack = new Stack<string>();
                    sessionThreads.Add(currentThread, callsStack);
                }

                callsStack.Push(methodName);
            }
        }

        /// <summary>
        /// Leaves the method.
        /// </summary>
        /// <param name="session">The session.</param>
        public void LeaveMethod(Session session)
        {
            if (LoadService.IsLoading) return;
            lock (callRegistry)
            {
                Dictionary<Thread, Stack<string>> sessionThreads;
                if (!callRegistry.TryGetValue(session, out sessionThreads)) return;

                var currentThread = Thread.CurrentThread;
                Stack<string> callsStack;
                if (!sessionThreads.TryGetValue(currentThread, out callsStack)) return;

                if (callsStack.Count > 0)
                    callsStack.Pop();

                if (callsStack.Count != 0) return;

                sessionThreads.Remove(currentThread);
                if (sessionThreads.Count == 0)
                    callRegistry.Remove(session);
            }
        }

        /// <summary>
        /// Gets the audit trail module.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns></returns>
        private AuditTrailModule getAuditTrailModule(XafApplication application)
        {
            return (from fieldInfo in application.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    where fieldInfo.FieldType == typeof(AuditTrailModule)
                    select (AuditTrailModule)fieldInfo.GetValue(application)).FirstOrDefault();
        }

    }
}

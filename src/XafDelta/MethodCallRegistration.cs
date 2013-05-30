using System;
using DevExpress.Xpo;

namespace XafDelta
{
    /// <summary>
    /// Service object for method call registration
    /// </summary>
    public sealed class MethodCallRegistration: IDisposable
    {
        /// <summary>
        /// Gets the session.
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodCallRegistration"/> class.
        /// Register method call.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="callContext">The call context (Type for static methods, Object for instance one).</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">Call arguments.</param>
        public MethodCallRegistration(Session session, object callContext, string methodName, params object[] args)
        {
            Session = session;
            XafDeltaModule.Instance.ProtocolService.EnterMethod(session, callContext, methodName, args);
        }

        #region IDisposable

        private bool isDisposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            XafDeltaModule.Instance.ProtocolService.LeaveMethod(Session);
        }

        #endregion
    }
}

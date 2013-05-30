using System;
using System.Collections.Generic;
using System.Linq;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Tracked object bag
    /// </summary>
    internal sealed class TrackedObjectBag : IDisposable
    {
        public SessionData SessionData { get; private set; }

        public TrackedObjectBag(SessionData sessionData)
        {
            SessionData = sessionData;
        }

        private readonly Dictionary<object, TrackedObjectData> trackedObjects =
            new Dictionary<object, TrackedObjectData>();

        private TrackedObjectData getObjectData(object source)
        {
            TrackedObjectData trackedObjectData;
            if (!trackedObjects.TryGetValue(source, out trackedObjectData))
            {
                trackedObjectData = TrackedObjectData.CreateTrackedData(source, this);
                if(trackedObjectData != null)
                    trackedObjects.Add(source, trackedObjectData);
            }
            return trackedObjectData;
        }

        /// <summary>
        /// Refreshes all tracked objects data.
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < trackedObjects.Values.Count; i++)
            {
                trackedObjects.Values.ElementAt(i).Refresh();
            }
        }

        /// <summary>
        /// Removes the specified source object.
        /// </summary>
        /// <param name="source">The source.</param>
        public void Remove(object source)
        {
            if (!trackedObjects.ContainsKey(source)) return;
            trackedObjects[source].Dispose();
            trackedObjects.Remove(source);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            while (trackedObjects.Count > 0)
                Remove(trackedObjects.ElementAt(0).Key);
        }

        /// <summary>
        /// Refreshes the specified tracked object data.
        /// </summary>
        /// <param name="source">The source.</param>
        public void Refresh(object source)
        {
            var objData = getObjectData(source);
            if(objData != null)
                objData.Refresh();
        }

        /// <summary>
        /// Gets the old values list.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Dictionary with old values</returns>
        public Dictionary<string, object> GetOldValues(object source)
        {
            Dictionary<string, object> result = null;
            var objData = getObjectData(source);
            if (objData != null)
                result = objData.OldValues;
            return result;
        }

        /// <summary>
        /// Gets the property old value.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Property old value or null</returns>
        public object GetPropertyOldValue(object source, string propertyName)
        {
            var oldValues = GetOldValues(source);
            object result = null;
            if (oldValues != null)
                oldValues.TryGetValue(propertyName, out result);
            return result;
        }

        /// <summary>
        /// Gets the replication key old value.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Replication key old value</returns>
        public string GetReplicationKeyOldValue(object source)
        {
            string result = null;
            var replKeyMember = ExtensionsHelper.GetReplicationKeyMember(source);
            if (replKeyMember != null)
            {
                var oldValues = GetOldValues(source);
                if (oldValues != null)
                {
                    object keyValue;
                    oldValues.TryGetValue(replKeyMember.Name, out keyValue);
                    if (keyValue != null)
                        result = keyValue.ToString();
                }
            }
            return result;
        }

        /// <summary>
        /// Sets property old value.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="newValue">The new value.</param>
        public void SetPropertyOldValue(object source, string propertyName, object newValue)
        {
            var oldValues = GetOldValues(source);
            if (oldValues != null)
            {
                if (oldValues.ContainsKey(propertyName))
                    oldValues[propertyName] = newValue;
                else
                    oldValues.Add(propertyName, newValue);
            }
        }

        #region IDisposable

        private bool isDisposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            Clear();
            isDisposed = true;
        }

        #endregion
    }
}
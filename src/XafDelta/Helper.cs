using System;
using System.IO;
using System.Reflection;

namespace XafDelta
{
    /// <summary>
    /// Helper static methods (includes method extensions)
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Copies sourceStream to destStream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="destStream">The destination stream.</param>
        /// <param name="leaveOpen">if set to <c>true</c> live source opened.</param>
        /// <returns>The destination stream</returns>
        public static Stream CopyTo(this Stream sourceStream, Stream destStream, bool leaveOpen)
        {
            var buffer = new byte[1024*1024];
            int readCount;
            while ((readCount = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                destStream.Write(buffer, 0, readCount);
            if (!leaveOpen)
                sourceStream.Close();
            return destStream;
        }

        /// <summary>
        /// Copies sourceStream to destStream and close sourceStream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="destStream">The dest stream.</param>
        /// <returns>The destination stream</returns>
        public static Stream CopyTo(this Stream sourceStream, Stream destStream)
        {
            return CopyTo(sourceStream, destStream, false);
        }

        /// <summary>
        /// Get all bytes from stream and close it
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <returns>All bytes from <paramref name="sourceStream"/></returns>
        public static byte[] AllBytes(this Stream sourceStream)
        {
            return AllBytes(sourceStream, false);
        }

        /// <summary>
        /// Get all the bytes from stream
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="leaveOpen">if set to <c>true</c> then leave stream open.</param>
        /// <returns>All bytes from <paramref name="sourceStream"/></returns>
        public static byte[] AllBytes(this Stream sourceStream, bool leaveOpen)
        {
            byte[] result;
            using (var memStream = new MemoryStream())
            {
                sourceStream.CopyTo(memStream, leaveOpen);
                memStream.Close();
                result = memStream.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Determines whether the specified type has attribute.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type has attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAttribute<T>(this Type type) where T: Attribute
        {
            return type.GetCustomAttributes(typeof (T), true).Length > 0;
        }

        /// <summary>
        /// Determines whether the specified member has attribute.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="member">The member.</param>
        /// <returns>
        ///   <c>true</c> if the specified member has attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAttribute<T>(this MemberInfo member) where T : Attribute
        {
            return member.GetCustomAttributes(typeof(T), true).Length > 0;
        }

        /// <summary>
        /// Determines whether the specified member has attribute.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="member">The member.</param>
        /// <returns>
        ///   <c>true</c> if the specified member has attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAttribute<T>(this PropertyInfo member) where T : Attribute
        {
            return member.GetCustomAttributes(typeof(T), true).Length > 0;
        }

        /// <summary>
        /// Convert XPO datetime value to local datetime
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Local datetime</returns>
        public static DateTime XpoToLocal(this DateTime value)
        {
            return value == DateTime.MinValue ? value : value.ToLocalTime();
        }

        /// <summary>
        /// Gets the property value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceObject">The source object.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Specified property value</returns>
        public static T GetPropertyValue<T>(object sourceObject, string propertyName)
        {
            T result = default(T);
            if(sourceObject != null && !string.IsNullOrEmpty(propertyName))
            {
                var propInfo = sourceObject.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if(propInfo != null)
                {
                    result = (T)propInfo.GetValue(sourceObject, null);
                }
            }
            return result;
        }
    }
}
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using DevExpress.Xpo;

namespace XafDelta
{
    /// <summary>
    /// BLOB converter
    /// </summary>
    internal static class ValueTransform
    {
        public const int MinBlobStringLength = 1024;

        /// <summary>
        /// Converts object to BLOB.
        /// </summary>
        /// <param name="sourceObject">The source object.</param>
        /// <returns>
        /// byte array if source have to be converted to blob; otherwise null
        /// </returns>
        public static byte[] ConvertToBlob(object sourceObject)
        {
            byte[] result = null;
            if (isValidBlobSource(sourceObject))
                // convert source object to BLOB using BinaryFormatter serialization
                using (var memoryStream = new MemoryStream())
                {
                    (new BinaryFormatter()).Serialize(memoryStream, sourceObject);
                    memoryStream.Close();
                    result = memoryStream.ToArray();
                }
            return result;
        }

        /// <summary>
        /// Restores object from BLOB.
        /// </summary>
        /// <param name="sourceBlob">The source BLOB.</param>
        /// <returns>object used to be source for BLOB</returns>
        public static object RestoreFromBlob(byte[] sourceBlob)
        {
            object result = null;
            // restore object from BLOB using BinaryFormatter desirialization
            if (sourceBlob != null)
                using (var memoryStream = new MemoryStream(sourceBlob))
                    result = (new BinaryFormatter()).Deserialize(memoryStream);
            return result;
        }

        /// <summary>
        /// Determines whether the specified source object is valid BLOB source.
        /// </summary>
        /// <param name="sourceObject">The source object.</param>
        /// <returns>
        ///   <c>true</c> if the specified source object is valid BLOB source; otherwise, <c>false</c>.
        /// </returns>
        private static bool isValidBlobSource(object sourceObject)
        {
            return sourceObject != null 
                && ((sourceObject is byte[]) 
                || (sourceObject.GetType().HasAttribute<SerializableAttribute>() 
                && (!(sourceObject is string) || sourceObject.ToString().Length >= MinBlobStringLength) 
                && !(sourceObject is IXPObject) 
                && !(sourceObject is ValueType)));
        }

        /// <summary>
        /// Convert object to string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string ObjectToString(object value)
        {
            string result = null;
            if(value != null)
            {
                if (value is DateTime)
                    result = ((DateTime)value).ToString("o");
                else if (value.GetType().IsEnum)
                    result = Enum.GetName(value.GetType(), value);
                else if (value is Color)
                    result = ((Color)value).ToArgb().ToString();
                else
                    result = Convert.ToString(value, CultureInfo.InvariantCulture);

                if (result.Length > MinBlobStringLength)
                    result = result.Substring(0, MinBlobStringLength);
            }
            return result;
        }

        /// <summary>
        /// Convert string to object.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <returns></returns>
        public static object StringToObject(string value, Type resultType)
        {
            object result = null;
            if (value != null && resultType != null)
            {
                if (typeof(DateTime) == resultType)
                    result = DateTime.ParseExact(value, "o", CultureInfo.InvariantCulture);
                else if (resultType.IsEnum)
                    result = Enum.Parse(resultType, value);
                else if (resultType == typeof(Color))
                    result = Color.FromArgb(int.Parse(value));
                else
                    result = Convert.ChangeType(value, resultType, CultureInfo.InvariantCulture);
            }
            return result;
        }
    }
}
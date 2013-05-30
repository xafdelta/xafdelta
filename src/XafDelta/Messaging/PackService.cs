#region

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace XafDelta.Messaging
{
    /// <summary>
    /// Packing/unpacking service for messages.
    /// For internal use only.
    /// </summary>
    internal sealed class PackService: BaseService
    {
        public PackService(XafDeltaModule owner) : base(owner)
        {
        }

        /// <summary>
        /// Packs the specified source stream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId"></param>
        /// <returns>Packed stream</returns>
        public byte[] PackStream(Stream sourceStream, string nodeId)
        {
            var args = new EventArgs();
            Owner.DoBeforePack(args);
            var result = Encrypt(Compress(sourceStream, nodeId), nodeId);
            Owner.DoAfterPack(args);
            return result.AllBytes();
        }

        /// <summary>
        /// Unpacks the specified source stream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId"></param>
        /// <returns>Unpacked stream</returns>
        public byte[] UnpackStream(Stream sourceStream, string nodeId)
        {
            var args = new EventArgs();
            Owner.DoBeforeUnpack(args);
            var result = Decompress(Decrypt(sourceStream, nodeId), nodeId);
            Owner.DoAfterUnpack(args);
            return result.AllBytes();
        }

        /// <summary>
        /// Packs the bytes.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="nodeId"></param>
        /// <returns>Packed bytes</returns>
        public byte[] PackBytes(byte[] sourceBytes, string nodeId)
        {
            byte[] result;
            using(var memStream = new MemoryStream(sourceBytes))
            {
                result = PackStream(memStream, nodeId);
            }
            return result;
        }

        /// <summary>
        /// Unpacks the bytes.
        /// </summary>
        /// <param name="sourceBytes">The source bytes.</param>
        /// <param name="nodeId"></param>
        /// <returns>Unpacked bytes</returns>
        public byte[] UnpackBytes(byte[] sourceBytes, string nodeId)
        {
            byte[] result;
            using (var memStream = new MemoryStream(sourceBytes))
            {
                result = UnpackStream(memStream, nodeId);
            }
            return result;
        }


        /// <summary>
        /// Encrypts the specified source stream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId"></param>
        /// <returns>Encrypted stream</returns>
        public Stream Encrypt(Stream sourceStream, string nodeId)
        {
            var alg = createAlgorithm();
            // try custom encryption
            var args = new CryptoEventArgs(sourceStream, nodeId, alg);
            Owner.DoBeforeEncrypt(args);
            // if no custom encryption then do default
            if (!args.Done)
            {
                if (string.IsNullOrEmpty(Owner.Password))
                {
                    sourceStream.CopyTo(args.DestStream);
                }
                else
                {
                    using (var ms = new MemoryStream())
                    using (var cryStream = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        sourceStream.CopyTo(cryStream);
                        cryStream.Close();
                        var bytes = ms.ToArray();
                        args.DestStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            args.DestStream.Seek(0, SeekOrigin.Begin);
            Owner.DoAfterEncrypt(args);
            return args.DestStream;
        }

        /// <summary>
        /// Compresses the specified source stream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId"></param>
        /// <returns>Compressed stream</returns>
        public Stream Compress(Stream sourceStream, string nodeId)
        {
            var args = new StreamEventArgs(sourceStream, nodeId);
            // try custom compression
            Owner.DoBeforeCompress(args);
            // if no custom compression then do default
            if (!args.Done)
            {
                using (var zipStream = new GZipStream(args.DestStream, CompressionMode.Compress, true))
                {
                    sourceStream.CopyTo(zipStream);
                    zipStream.Close();
                }
            }
            args.DestStream.Seek(0, SeekOrigin.Begin);
            Owner.DoAfterCompress(args);
            return args.DestStream;
        }

        /// <summary>
        /// Decrypts the specified source stream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId"></param>
        /// <returns>Decrypted stream</returns>
        public Stream Decrypt(Stream sourceStream, string nodeId)
        {
            var alg = createAlgorithm();
            var args = new CryptoEventArgs(sourceStream, nodeId, alg);
            // try custom decryption
            Owner.DoBeforeDecrypt(args);
            // if no custom decryption then do default
            if (!args.Done)
            {
                if (string.IsNullOrEmpty(Owner.Password))
                {
                    sourceStream.CopyTo(args.DestStream);
                }
                else
                {
                    using (var cryStream = new CryptoStream(sourceStream, alg.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        cryStream.CopyTo(args.DestStream);
                        sourceStream.Close();
                    }
                }
            }
            args.DestStream.Seek(0, SeekOrigin.Begin);
            Owner.DoAfterDecrypt(args);
            return args.DestStream;
        }

        /// <summary>
        /// Decompresses the specified source stream.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId"></param>
        /// <returns>Decompressed stream</returns>
        public Stream Decompress(Stream sourceStream, string nodeId)
        {
            var args = new StreamEventArgs(sourceStream, nodeId);
            // try custom decompression
            Owner.DoBeforeDecompress(args);
            // if no custom decompression then do default
            if (!args.Done)
            {
                using (var zipStream = new GZipStream(sourceStream, CompressionMode.Decompress, false))
                {
                    zipStream.CopyTo(args.DestStream);
                }
            }
            args.DestStream.Seek(0, SeekOrigin.Begin);
            Owner.DoAfterDecompress(args);
            return args.DestStream;
        }

        /// <summary>
        /// Creates and initialize crypto algorithm.
        /// </summary>
        /// <returns>Symmetric crypto algorithm</returns>
        private SymmetricAlgorithm createAlgorithm()
        {
            var result =
                (SymmetricAlgorithm) Activator.CreateInstance(GetCryptoAlgorithmType(Owner.CryptoAlgorithmType));
            // password hash is a base for iv and key
            var hash = (new SHA512Managed()).ComputeHash(Encoding.UTF8.GetBytes(Owner.Password));

            var iv = new byte[result.BlockSize/8];
            Array.Reverse(hash);
            Array.ConstrainedCopy(hash, 0, iv, 0, iv.Length);
            result.IV = iv;

            // assume key length is maximal for selected algorithm
            var keySize = (from c in result.LegalKeySizes select c.MaxSize).Max()/8;
            var key = new byte[keySize];
            Array.Reverse(hash);
            Array.ConstrainedCopy(hash, 0, key, 0, key.Length);
            result.Key = key;

            return result;
        }

        /// <summary>
        ///   Converts algorithm type into algorithm implementation class
        /// </summary>
        /// <param name = "algorithmType">Type of the algorithm.</param>
        /// <returns>Crypto algorithm type</returns>
        public static Type GetCryptoAlgorithmType(CryptoAlgorithmType algorithmType)
        {
            Type result = null;
            switch (algorithmType)
            {
                case CryptoAlgorithmType.RijndaelManaged:
                    result = typeof (RijndaelManaged);
                    break;

                case CryptoAlgorithmType.Des:
                    result = typeof (DESCryptoServiceProvider);
                    break;

                case CryptoAlgorithmType.Rc2:
                    result = typeof (RC2CryptoServiceProvider);
                    break;

                case CryptoAlgorithmType.TripleDes:
                    result = typeof (TripleDESCryptoServiceProvider);
                    break;
            }
            return result;
        }
    }

    /// <summary>
    ///   Standard CryptoAlgorithm types
    /// </summary>
    public enum CryptoAlgorithmType
    {
        /// <summary>
        ///   RijndaelManaged symmetric crypto algorithm
        /// </summary>
        RijndaelManaged,

        /// <summary>
        ///   DESCryptoServiceProvider symmetric crypto algorithm
        /// </summary>
        Des,

        /// <summary>
        ///   RC2CryptoServiceProvider symmetric crypto algorithm
        /// </summary>
        Rc2,

        /// <summary>
        ///   TripleDESCryptoServiceProvider symmetric crypto algorithm
        /// </summary>
        TripleDes
    }

    #region Event args

    /// <summary>
    /// Stream transforming event arguments
    /// </summary>
    public class StreamEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamEventArgs"/> class.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId">The node id.</param>
        public StreamEventArgs(Stream sourceStream, string nodeId)
        {
            DestStream = new MemoryStream();
            SourceStream = sourceStream;
            NodeId = nodeId;
        }

        /// <summary>
        /// Gets the source stream, contains initial data.
        /// </summary>
        public Stream SourceStream { get; private set; }

        /// <summary>
        /// Gets the destination stream. Processed data should be written to DestStream.
        /// </summary>
        public MemoryStream DestStream { get; private set; }

        /// <summary>
        /// Gets the replication node Id.
        /// </summary>
        public string NodeId { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether operation is compleated by user in event handler.
        /// Set this property to <c>true</c> to disable standard processing.
        /// </summary>
        /// <value>
        ///   <c>true</c> if done; otherwise, <c>false</c>.
        /// </value>
        public bool Done { get; set; }
    }

    /// <summary>
    /// Crypto events arguments
    /// </summary>
    public class CryptoEventArgs : StreamEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoEventArgs"/> class.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="nodeId">The node id.</param>
        /// <param name="algoritm">The algoritm.</param>
        public CryptoEventArgs(Stream sourceStream, string nodeId,
                               SymmetricAlgorithm algoritm)
            : base(sourceStream, nodeId)
        {
            Algoritm = algoritm;
        }

        /// <summary>
        /// Gets or sets the symmetric cryptographic algorithm used for encryption (decryption). 
        /// For <see cref="XafDeltaModule.BeforeEncrypt"/> and <see cref="XafDeltaModule.BeforeDecrypt"/> events 
        /// you can customize algorithm (change Key, Iv or other properties) or even create new one.
        /// </summary>
        /// <value>
        /// The symmetric crypto algoritm.
        /// </value>
        public SymmetricAlgorithm Algoritm { get; set; }
    }

    #endregion
}
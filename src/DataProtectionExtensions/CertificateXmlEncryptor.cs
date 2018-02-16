using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataProtectionExtensions
{
	/// <summary>
	/// XML encryptor that uses a provided certificate public key
	/// to handle encryption operations.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlEncryptor" />
	[SuppressMessage("CA2213", "CA2213", Justification = "The RSA key provider is disposed with the certificate.")]
	public class CertificateXmlEncryptor : IXmlEncryptor, IDisposable
	{
		/// <summary>
		/// The ID of the element that contains the encrypted data. Used to tie
		/// the element to decryption information.
		/// </summary>
		private const string EncryptedElementId = "certificateEncryptedKey";

		/// <summary>
		/// The length of the session key used to encrypt data. Given the session
		/// key itself is encrypted, this doesn't have to be quite as large
		/// as the master key.
		/// </summary>
		private const int SessionKeySize = 256;

		/// <summary>
		/// The name of the key used to encrypt the XML. This is the
		/// certificate thumbprint. If this doesn't match the name of
		/// the key used to decrypt the XML, the decryption will fail.
		/// </summary>
		private readonly string _keyName;

		/// <summary>
		/// The key provider containing the public key of the certificate
		/// used to encrypt data.
		/// </summary>
		private readonly RSA _keyProvider;

		/// <summary>
		/// The session key used to encrypt data. The master certificate key
		/// is used to encrypt the session key; the session key is used to
		/// encrypt the data.
		/// </summary>
		private readonly RijndaelManaged _sessionKey;

		/// <summary>
		/// Flag indicating whether the object has been disposed.
		/// </summary>
		private bool _disposed = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateXmlEncryptor"/> class.
		/// </summary>
		/// <param name="serviceProvider">
		/// The <see cref="IServiceProvider"/> that will be used to locate
		/// required services.
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the <paramref name="serviceProvider" /> does not have an
		/// <see cref="ILogger{T}"/> for this class; or if there is no <see cref="CertificateEncryptionOptions"/>
		/// registered.
		/// </exception>
		public CertificateXmlEncryptor(IServiceProvider serviceProvider)
		{
			// You have to use service location like this because
			// there are a lot of bugs and shortcomings in the way
			// DI is integrated into the data protection mechanism.
			// They don't support constructor injection.
			// https://github.com/aspnet/Home/issues/2523
			this.Logger = serviceProvider.GetRequiredService<ILogger<CertificateXmlEncryptor>>();
			var options = serviceProvider.GetRequiredService<CertificateEncryptionOptions>();

			// Get the data we need out of the certificate and we don't
			// have to keep a reference to the whole thing.
			this._keyProvider = options.Certificate.GetRSAPublicKey();
			this._keyName = options.Certificate.Thumbprint;

			// Create a session key to encrypt the data. This will be included
			// and encrypted inside the overall package.
			this._sessionKey = new RijndaelManaged
			{
				KeySize = SessionKeySize,
			};
		}

		/// <summary>
		/// Gets the logger.
		/// </summary>
		/// <value>
		/// An <see cref="ILogger{T}"/> used to log diagnostic messages.
		/// </value>
		public ILogger<CertificateXmlEncryptor> Logger { get; private set; }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing,
		/// or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Encrypts the specified XML element.
		/// </summary>
		/// <param name="plaintextElement">The plaintext to encrypt.</param>
		/// <returns>
		/// An <see cref="EncryptedXmlInfo" /> that contains the encrypted value of
		/// <paramref name="plaintextElement" /> along with information about how to
		/// decrypt it.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="plaintextElement" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		/// Thrown if this method is called after <see cref="Dispose()"/> is called.
		/// </exception>
		public EncryptedXmlInfo Encrypt(XElement plaintextElement)
		{
			if (plaintextElement == null)
			{
				throw new ArgumentNullException(nameof(plaintextElement));
			}

			if (this._disposed)
			{
				throw new ObjectDisposedException("Unable to encrypt after the object has been disposed.");
			}

			this.Logger.LogDebug("Encrypting XML with certificate {0}.", this._keyName);

			// Create a faux XML document from the XElement so we can use EncryptedXml.
			var xmlDocument = plaintextElement.ToXmlDocumentWithRootNode();
			var elementToEncrypt = xmlDocument.ElementToProcess();

			// Do the actual encryption. Algorithm based on MSDN docs:
			// https://msdn.microsoft.com/en-us/library/ms229746(v=vs.110).aspx
			var encryptedXml = new EncryptedXml();
			var encryptedElement = encryptedXml.EncryptData(elementToEncrypt, this._sessionKey, false);

			// Build the wrapper elements that provide information about
			// the algorithms used, the name of the key used, and so on.
			var encryptedData = new EncryptedData
			{
				Type = EncryptedXml.XmlEncElementUrl,
				Id = EncryptedElementId,
				EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url),
			};

			var encryptedKey = new EncryptedKey
			{
				CipherData = new CipherData(EncryptedXml.EncryptKey(this._sessionKey.Key, this._keyProvider, false)),
				EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSA15Url),
			};

			// "Connect" the encrypted data and encrypted key with
			// element references.
			var encryptedElementDataReference = new DataReference
			{
				Uri = "#" + EncryptedElementId,
			};
			encryptedKey.AddReference(encryptedElementDataReference);
			encryptedData.KeyInfo.AddClause(new KeyInfoEncryptedKey(encryptedKey));

			var keyName = new KeyInfoName
			{
				Value = this._keyName,
			};
			encryptedKey.KeyInfo.AddClause(keyName);

			encryptedData.CipherData.CipherValue = encryptedElement;

			// Swap the plaintext element for the encrypted element.
			EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);

			return new EncryptedXmlInfo(xmlDocument.ElementToProcess().ToXElement(), typeof(CertificateXmlDecryptor));
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing">
		/// <see langword="true" /> to release both managed and unmanaged resources;
		/// <see langword="false" /> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					this._sessionKey.Dispose();
				}

				this._disposed = true;
			}
		}
	}
}

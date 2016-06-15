using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataProtectionExtensions
{
	/// <summary>
	/// XML decryptor that uses a provided certificate private key
	/// to handle decryption operations.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.DataProtection.XmlEncryption.IXmlDecryptor" />
	[SuppressMessage("CA2213", "CA2213", Justification = "The RSA key provider is disposed with the certificate.")]
	public class CertificateXmlDecryptor : IXmlDecryptor
	{
		/// <summary>
		/// The name of the key used to decrypt the XML. This is the
		/// certificate thumbprint. If this doesn't match the name of
		/// the key used to encrypt the XML, the decryption will fail.
		/// </summary>
		private readonly string _keyName;

		/// <summary>
		/// The key provider containing the private key of the certificate
		/// used to decrypt data.
		/// </summary>
		private readonly RSACryptoServiceProvider _keyProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateXmlDecryptor"/> class.
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
		public CertificateXmlDecryptor(IServiceProvider serviceProvider)
		{
			// You have to use service location like this because
			// there are a lot of bugs and shortcomings in the way
			// DI is integrated into the data protection mechanism.
			// They don't support constructor injection.
			// https://github.com/aspnet/DataProtection/issues/134
			// https://github.com/aspnet/DataProtection/issues/154
			this.Logger = serviceProvider.GetRequiredService<ILogger<CertificateXmlDecryptor>>();
			var options = serviceProvider.GetRequiredService<CertificateEncryptionOptions>();

			this._keyProvider = (RSACryptoServiceProvider)options.Certificate.PrivateKey;
			this._keyName = options.Certificate.Thumbprint;
		}

		/// <summary>
		/// Gets the logger.
		/// </summary>
		/// <value>
		/// An <see cref="ILogger{T}"/> used to log diagnostic messages.
		/// </value>
		public ILogger<CertificateXmlDecryptor> Logger { get; private set; }

		/// <summary>
		/// Decrypts the specified XML element.
		/// </summary>
		/// <param name="encryptedElement">
		/// An encrypted XML element.
		/// </param>
		/// <returns>
		/// The decrypted form of <paramref name="encryptedElement" />.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="encryptedElement" /> is <see langword="null" />.
		/// </exception>
		public XElement Decrypt(XElement encryptedElement)
		{
			if (encryptedElement == null)
			{
				throw new ArgumentNullException(nameof(encryptedElement));
			}

			this.Logger.LogDebug("Decrypting XML with certificate {0}.", this._keyName);

			// Create a faux XML document from the XElement so we can use EncryptedXml.
			var xmlDocument = encryptedElement.ToXmlDocumentWithRootNode();

			// Do the actual decryption. Algorithm based on MSDN docs:
			// https://msdn.microsoft.com/en-us/library/ms229746(v=vs.110).aspx
			var encryptedXml = new EncryptedXml(xmlDocument);
			encryptedXml.AddKeyNameMapping(this._keyName, this._keyProvider);

			try
			{
				encryptedXml.DecryptDocument();
			}
			catch (CryptographicException ex) when (ex.Message.IndexOf("bad key", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				// If you get a CryptographicException with the message "Bad Key"
				// in it here, it means the certificate used to encrypt wasn't generated
				// with "-sky Exchange" in makecert.exe so the encrypt/decrypt functionality
				// isn't enabled for it.
				this.Logger.LogError("Bad key exception was encountered. Did you generate the certificate with '-sky Exchange' to enable encryption/decryption?");
				throw;
			}

			return xmlDocument.ElementToProcess().ToXElement();
		}
	}
}

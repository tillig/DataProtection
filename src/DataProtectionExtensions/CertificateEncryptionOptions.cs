using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DataProtectionExtensions
{
	/// <summary>
	/// Options used in certificate-based key encryption for data protection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is valuable so you can register a certificate for use with
	/// the <see cref="CertificateXmlDecryptor"/> and <see cref="CertificateXmlEncryptor"/>
	/// without registering an <see cref="X509Certificate2"/> directly in the
	/// DI container. That way if other functions in the system also need certificates
	/// there won't be any need to disambiguate.
	/// </para>
	/// </remarks>
	public class CertificateEncryptionOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateEncryptionOptions"/> class.
		/// </summary>
		/// <param name="certificate">
		/// The <see cref="X509Certificate2"/> that contains the asymmetric key pair
		/// used to encrypt and decrypt the rotating data protection keys.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="certificate" /> is <see langword="null" />.
		/// </exception>
		public CertificateEncryptionOptions(X509Certificate2 certificate)
		{
			if (certificate == null)
			{
				throw new ArgumentNullException(nameof(certificate));
			}

			this.Certificate = certificate;
		}

		/// <summary>
		/// Gets the encryption certificate.
		/// </summary>
		/// <value>
		/// The <see cref="X509Certificate2"/> that contains the asymmetric key pair
		/// used to encrypt and decrypt the rotating data protection keys.
		/// </value>
		public X509Certificate2 Certificate { get; private set; }
	}
}

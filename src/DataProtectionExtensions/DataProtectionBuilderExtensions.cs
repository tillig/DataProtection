using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DataProtectionExtensions
{
	/// <summary>
	/// Extension methods for <see cref="IDataProtectionBuilder"/> for configuring
	/// data protection options.
	/// </summary>
	public static class DataProtectionBuilderExtensions
	{
		/// <summary>
		/// Sets up data protection to protect session keys with a provided certificate.
		/// </summary>
		/// <param name="builder">The <see cref="IDataProtectionBuilder"/> used to set up data protection options.</param>
		/// <param name="certificate">The certificate to use for session key encryption.</param>
		/// <returns>
		/// The <paramref name="builder" /> for continued configuration.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="builder" /> or <paramref name="certificate" /> is <see langword="null" />.
		/// </exception>
		/// <remarks>
		/// <para>
		/// The standard certificate encryption allows you to pass in a certificate for
		/// encryption, but during decryption requires the certificate to be in the
		/// machine certificate store. This version uses only the certificate provided
		/// and does not look at the certificate store.
		/// </para>
		/// </remarks>
		public static IDataProtectionBuilder ProtectKeysWithProvidedCertificate(this IDataProtectionBuilder builder, X509Certificate2 certificate)
		{
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			if (certificate == null)
			{
				throw new ArgumentNullException(nameof(certificate));
			}

			builder.Services
				.AddSingleton<CertificateXmlEncryptor>()
				.AddSingleton(new CertificateEncryptionOptions(certificate))
				.AddSingleton<IConfigureOptions<KeyManagementOptions>>(provider =>
			{
				return new ConfigureOptions<KeyManagementOptions>(opts =>
				{
					opts.XmlEncryptor = provider.GetRequiredService<CertificateXmlEncryptor>();
				});
			});

			return builder;
		}
	}
}

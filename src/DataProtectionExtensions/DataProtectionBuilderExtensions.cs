using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataProtectionExtensions
{
	/// <summary>
	/// Extension methods for <see cref="IDataProtectionBuilder"/> for configuring
	/// data protection options.
	/// </summary>
	public static class DataProtectionBuilderExtensions
	{
		/// <summary>
		/// Sets up data protection to persist session keys in Redis.
		/// </summary>
		/// <param name="builder">The <see cref="IDataProtectionBuilder"/> used to set up data protection options.</param>
		/// <param name="redisConnectionString">The connection string specifying the Redis instance and database for key storage.</param>
		/// <returns>
		/// The <paramref name="builder" /> for continued configuration.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="builder" /> or <paramref name="redisConnectionString" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// Thrown if <paramref name="redisConnectionString" /> is empty.
		/// </exception>
		public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder, string redisConnectionString)
		{
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			if (redisConnectionString == null)
			{
				throw new ArgumentNullException(nameof(redisConnectionString));
			}

			if (redisConnectionString.Length == 0)
			{
				throw new ArgumentException("Redis connection string may not be empty.", nameof(redisConnectionString));
			}

			return builder.Use(ServiceDescriptor.Singleton<IXmlRepository>(services => new RedisXmlRepository(redisConnectionString, services.GetRequiredService<ILogger<RedisXmlRepository>>())));
		}

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

			var options = new CertificateEncryptionOptions(certificate);
			return builder
				.Use(new ServiceDescriptor(typeof(CertificateEncryptionOptions), options))
				.Use(new ServiceDescriptor(typeof(IXmlEncryptor), typeof(CertificateXmlEncryptor), ServiceLifetime.Singleton))
				.Use(new ServiceDescriptor(typeof(IXmlDecryptor), typeof(CertificateXmlDecryptor), ServiceLifetime.Singleton));
		}

		/// <summary>
		/// Updates an <see cref="IDataProtectionBuilder"/> to use the service of
		/// a specific type, removing all other services of that type.
		/// </summary>
		/// <param name="builder">The <see cref="IDataProtectionBuilder"/> that should use the specified service.</param>
		/// <param name="descriptor">The <see cref="ServiceDescriptor"/> with the service the <paramref name="builder" /> should use.</param>
		/// <returns>
		/// The <paramref name="builder" /> for continued configuration.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="builder" /> or <paramref name="descriptor" /> is <see langword="null" />.
		/// </exception>
		public static IDataProtectionBuilder Use(this IDataProtectionBuilder builder, ServiceDescriptor descriptor)
		{
			// This algorithm of removing all other services of a specific type
			// before adding the new/replacement service is how the base ASP.NET
			// DataProtection bits work. Due to some of the differences in how
			// that base set of bits handles DI, it's better to follow suit
			// and work in the same way than to try and debug weird issues.
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			if (descriptor == null)
			{
				throw new ArgumentNullException(nameof(descriptor));
			}

			for (int i = builder.Services.Count - 1; i >= 0; i--)
			{
				if (builder.Services[i]?.ServiceType == descriptor.ServiceType)
				{
					builder.Services.RemoveAt(i);
				}
			}

			builder.Services.Add(descriptor);
			return builder;
		}
	}
}

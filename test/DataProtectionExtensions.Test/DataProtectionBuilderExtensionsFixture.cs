using System;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class DataProtectionBuilderExtensionsFixture
	{
		[Fact]
		public void PersistKeysToRedis_EmptyConnectionString()
		{
			var builder = new DataProtectionBuilder(new ServiceCollection());
			Assert.Throws<ArgumentException>(() => DataProtectionBuilderExtensions.PersistKeysToRedis(builder, ""));
		}

		[Fact]
		public void PersistKeysToRedis_NullBuilder()
		{
			Assert.Throws<ArgumentNullException>(() => DataProtectionBuilderExtensions.PersistKeysToRedis(null, "connection"));
		}

		[Fact]
		public void PersistKeysToRedis_NullConnectionString()
		{
			var builder = new DataProtectionBuilder(new ServiceCollection());
			Assert.Throws<ArgumentNullException>(() => DataProtectionBuilderExtensions.PersistKeysToRedis(builder, null));
		}

		[Fact]
		public void PersistKeysToRedis_RegistersServices()
		{
			var builder = new DataProtectionBuilder(new ServiceCollection());
			builder.PersistKeysToRedis("connection");

			// A lambda factory gets registered for the repo so we can't test the type without actually
			// trying to connect to Redis.
			Assert.Single(builder.Services.Where(s => s.ServiceType == typeof(IXmlRepository)));
		}

		[Fact]
		public void ProtectKeysWithProvidedCertificate_NullBuilder()
		{
			Assert.Throws<ArgumentNullException>(() => DataProtectionBuilderExtensions.ProtectKeysWithProvidedCertificate(null, TestCertificate.GetCertificate()));
		}

		[Fact]
		public void ProtectKeysWithProvidedCertificate_NullCertificate()
		{
			var builder = new DataProtectionBuilder(new ServiceCollection());
			Assert.Throws<ArgumentNullException>(() => DataProtectionBuilderExtensions.ProtectKeysWithProvidedCertificate(builder, null));
		}

		[Fact]
		public void ProtectKeysWithProvidedCertificate_RegistersServices()
		{
			var builder = new DataProtectionBuilder(new ServiceCollection());
			var certificate = TestCertificate.GetCertificate();
			builder.ProtectKeysWithProvidedCertificate(certificate);

			Assert.Single(builder.Services.Where(s => s.ServiceType == typeof(CertificateEncryptionOptions)));
			Assert.Same(certificate, ((CertificateEncryptionOptions)builder.Services.First(s => s.ServiceType == typeof(CertificateEncryptionOptions)).ImplementationInstance).Certificate);
			Assert.Single(builder.Services.Where(s => s.ServiceType == typeof(IXmlEncryptor)));
			Assert.Equal(typeof(CertificateXmlEncryptor), builder.Services.First(s => s.ServiceType == typeof(IXmlEncryptor)).ImplementationType);
			Assert.Single(builder.Services.Where(s => s.ServiceType == typeof(IXmlDecryptor)));
			Assert.Equal(typeof(CertificateXmlDecryptor), builder.Services.First(s => s.ServiceType == typeof(IXmlDecryptor)).ImplementationType);
		}

		[Fact]
		public void Use_NullBuilder()
		{
			var descriptor = new ServiceDescriptor(typeof(string), "a");
			Assert.Throws<ArgumentNullException>(() => DataProtectionBuilderExtensions.Use(null, descriptor));
		}

		[Fact]
		public void Use_NullDescriptor()
		{
			var builder = new DataProtectionBuilder(new ServiceCollection());
			Assert.Throws<ArgumentNullException>(() => DataProtectionBuilderExtensions.Use(builder, null));
		}

		[Fact]
		public void Use_ReplacesAllServicesMatchingType()
		{
			var descriptor = new ServiceDescriptor(typeof(string), "c");
			IServiceCollection services = new ServiceCollection();
			services.Add(new ServiceDescriptor(typeof(string), "a"));
			services.Add(new ServiceDescriptor(typeof(string), "b"));
			var builder = new DataProtectionBuilder(services);
			builder.Use(descriptor);
			Assert.Single(services.Where(s => s.ServiceType == typeof(string)));
			Assert.Equal("c", services[0].ImplementationInstance);
		}
	}
}

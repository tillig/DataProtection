using System;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class DataProtectionBuilderExtensionsFixture
	{
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
		public void ProtectKeysWithProvidedCertificate_PreparesDecryptor()
		{
			var certificate = TestCertificate.GetCertificate();
			var services = new ServiceCollection();
			services
				.AddLogging()
				.AddDataProtection()
				.ProtectKeysWithProvidedCertificate(certificate);

			var provider = services.BuildServiceProvider();

			// Shouldn't throw.
			var decryptor = new CertificateXmlDecryptor(provider);
		}

		[Fact]
		public void ProtectKeysWithProvidedCertificate_SetsOptions()
		{
			var certificate = TestCertificate.GetCertificate();
			var services = new ServiceCollection();
			services
				.AddLogging()
				.AddDataProtection()
				.ProtectKeysWithProvidedCertificate(certificate);

			var provider = services.BuildServiceProvider();
			var options = provider.GetRequiredService<IOptions<KeyManagementOptions>>();
			Assert.IsType<CertificateXmlEncryptor>(options.Value.XmlEncryptor);
		}
	}
}

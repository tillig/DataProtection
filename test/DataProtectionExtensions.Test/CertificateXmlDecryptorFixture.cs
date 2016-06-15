using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class CertificateXmlDecryptorFixture
	{
		[Fact]
		public void CertificateXmlDecryptor_Integration()
		{
			// This verifies the round-trip through encryption/decrpytion
			// using a provided certificate.
			var toEncrypt = XElement.Parse("<node><child>value</child></node>");
			var encryptor = CreateEncryptor();
			var encrypted = encryptor.Encrypt(toEncrypt);
			var decryptor = CreateDecryptor();
			var decrypted = decryptor.Decrypt(encrypted.EncryptedElement);
			Assert.Equal("<node><child>value</child></node>", decrypted.ToString(SaveOptions.DisableFormatting));
		}

		[Fact]
		public void Ctor_MissingLogger()
		{
			var services = new ServiceCollection();
			var options = new CertificateEncryptionOptions(TestCertificate.GetCertificate());
			services.AddSingleton(options);
			var provider = services.BuildServiceProvider();
			Assert.Throws<InvalidOperationException>(() => new CertificateXmlDecryptor(provider));
		}

		[Fact]
		public void Ctor_MissingOptions()
		{
			var services = new ServiceCollection();
			var logger = Mock.Of<ILogger<CertificateXmlDecryptor>>();
			services.AddSingleton<ILogger<CertificateXmlDecryptor>>(logger);
			var provider = services.BuildServiceProvider();
			Assert.Throws<InvalidOperationException>(() => new CertificateXmlDecryptor(provider));
		}

		[Fact]
		public void Decrypt_NullElement()
		{
			var decryptor = CreateDecryptor();
			Assert.Throws<ArgumentNullException>(() => decryptor.Decrypt(null));
		}

		private static CertificateXmlDecryptor CreateDecryptor()
		{
			var logger = Mock.Of<ILogger<CertificateXmlDecryptor>>();
			var options = new CertificateEncryptionOptions(TestCertificate.GetCertificate());
			var services = new ServiceCollection();
			services.AddSingleton<ILogger<CertificateXmlDecryptor>>(logger);
			services.AddSingleton(options);
			var provider = services.BuildServiceProvider();
			return new CertificateXmlDecryptor(provider);
		}

		private static CertificateXmlEncryptor CreateEncryptor()
		{
			var logger = Mock.Of<ILogger<CertificateXmlEncryptor>>();
			var options = new CertificateEncryptionOptions(TestCertificate.GetCertificate());
			var services = new ServiceCollection();
			services.AddSingleton<ILogger<CertificateXmlEncryptor>>(logger);
			services.AddSingleton(options);
			var provider = services.BuildServiceProvider();
			return new CertificateXmlEncryptor(provider);
		}
	}
}

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class CertificateXmlEncryptorFixture
	{
		[Fact]
		public void Ctor_MissingLogger()
		{
			var services = new ServiceCollection();
			var options = new CertificateEncryptionOptions(TestCertificate.GetCertificate());
			services.AddSingleton(options);
			var provider = services.BuildServiceProvider();
			Assert.Throws<InvalidOperationException>(() => new CertificateXmlEncryptor(provider));
		}

		[Fact]
		public void Ctor_MissingOptions()
		{
			var services = new ServiceCollection();
			var logger = Mock.Of<ILogger<CertificateXmlDecryptor>>();
			services.AddSingleton<ILogger<CertificateXmlDecryptor>>(logger);
			var provider = services.BuildServiceProvider();
			Assert.Throws<InvalidOperationException>(() => new CertificateXmlEncryptor(provider));
		}

		[Fact]
		public void Dispose_MultipleCallsSucceed()
		{
			var encryptor = CreateEncryptor();
			encryptor.Dispose();
			encryptor.Dispose();
		}

		[Fact]
		public void Encrypt_DecryptorType()
		{
			var toEncrypt = XElement.Parse("<node><child>value</child></node>");
			var encryptor = CreateEncryptor();
			var encrypted = encryptor.Encrypt(toEncrypt);
			Assert.Equal(typeof(CertificateXmlDecryptor), encrypted.DecryptorType);
		}

		[Fact]
		public void Encrypt_FailsAfterDispose()
		{
			var toEncrypt = XElement.Parse("<node><child>value</child></node>");
			var encryptor = CreateEncryptor();
			encryptor.Dispose();
			Assert.Throws<ObjectDisposedException>(() => encryptor.Encrypt(toEncrypt));
		}

		[Fact]
		public void Encrypt_NullElement()
		{
			var encryptor = CreateEncryptor();
			Assert.Throws<ArgumentNullException>(() => encryptor.Encrypt(null));
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

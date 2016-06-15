using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using DataProtectionExtensions;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class CertificateEncryptionOptionsFixture
	{
		[Fact]
		public void Ctor_NullCertificate()
		{
			Assert.Throws<ArgumentNullException>(() => new CertificateEncryptionOptions(null));
		}

		[Fact]
		public void Ctor_SetsCertificate()
		{
			var cert = new X509Certificate2();
			var options = new CertificateEncryptionOptions(cert);
			Assert.Same(cert, options.Certificate);
		}
	}
}

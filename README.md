# DataProtection
Additional utilities and support for working with ASP.NET Core DataProtection.

This repo is provided as an example for code that can help get ASP.NET Core data protection working in a non-Azure farm environment.

Included:

- **XML encryption/decryption using a certificate that isn't required to be in a machine certificate store.** This allows you to store the master certificate in a repository like Azure Key Vault. The default XML encryption via certificates currently requires the certificate be in a machine certificate store for decryption.
- **Encrypted XML storage in Redis.** Well... this _was_ included until it was [finally added in the official packages](https://github.com/aspnet/DataProtection/blob/rel/1.1.0/src/Microsoft.AspNetCore.DataProtection.Redis/RedisXmlRepository.cs).
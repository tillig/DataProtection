# DataProtection
Additional utilities and support for working with ASP.NET Core DataProtection.

This repo is provided as an example for code that can help get ASP.NET Core data protection working in a non-Azure farm environment.

Included:

- **XML encryption/decryption using a certificate that isn't required to be in a machine certificate store.** This allows you to store the master certificate in a repository like Azure Key Vault. The default XML encryption via certificates currently requires the certificate be in a machine certificate store for decryption.
- **Encrypted XML storage in Redis.** Out of the box the ASP.NET DataProtection ships with the ability to share keys across a farm via file share; this adds a new centralized storage mechanism.
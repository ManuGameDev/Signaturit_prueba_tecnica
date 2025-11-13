using Domain.Ports;
using System.Security.Cryptography;

namespace Infrastructure.Cryptography
{
    /// <summary>
    /// Implementación de criptografía usando RSA-2048 y SHA-256
    /// </summary>
    public class RsaCryptographyService : ICryptographyService
    {
        public string ComputeHash(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return Convert.ToBase64String(hashBytes);
        }

        public byte[] Sign(byte[] data, string privateKeyPem)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));

            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new ArgumentException("Private key cannot be empty", nameof(privateKeyPem));

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);
                return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Error signing data: {ex.Message}", ex);
            }
        }

        public bool Verify(byte[] data, byte[] signature, string publicKeyPem)
        {
            if (data == null || data.Length == 0)
                return false;

            if (signature == null || signature.Length == 0)
                return false;

            if (string.IsNullOrWhiteSpace(publicKeyPem))
                return false;

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        public (string publicKey, string privateKey) GenerateKeyPair()
        {
            using var rsa = RSA.Create(2048);
            return (rsa.ExportRSAPublicKeyPem(), rsa.ExportRSAPrivateKeyPem());
        }
    }
}

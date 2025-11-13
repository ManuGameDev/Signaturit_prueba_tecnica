namespace Domain.Ports
{
    /// <summary>
    /// Puerto para servicios criptográficos (firma, verificación, hash)
    /// </summary>
    public interface ICryptographyService
    {
        /// <summary>
        /// Calcula el hash SHA256 de los datos
        /// </summary>
        string ComputeHash(byte[] data);

        /// <summary>
        /// Firma datos con una clave privada RSA
        /// </summary>
        byte[] Sign(byte[] data, string privateKeyPem);

        /// <summary>
        /// Verifica una firma RSA con clave pública
        /// </summary>
        bool Verify(byte[] data, byte[] signature, string publicKeyPem);

        /// <summary>
        /// Genera un par de claves RSA (pública y privada)
        /// </summary>
        (string publicKey, string privateKey) GenerateKeyPair();
    }
}

using Domain.Ports;

namespace Domain.Entities
{
    /// <summary>
    /// Representa una firma digital generada por el sistema
    /// </summary>
    public class Signature
    {
        public string DocumentId { get; private set; }
        public byte[] Value { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string SignedBy { get; private set; }
        public string Algorithm { get; private set; }

        private Signature() { }

        /// <summary>
        /// Crea una nueva firma
        /// </summary>
        public static Signature Create(string documentId, byte[] value, string signedBy)
        {
            return new Signature
            {
                DocumentId = documentId,
                Value = value,
                Timestamp = DateTime.UtcNow,
                SignedBy = signedBy,
                Algorithm = "RSA-SHA256"
            };
        }

        /// <summary>
        /// Verifica esta firma contra contenido original
        /// </summary>
        public bool Verify(byte[] originalContent, string publicKey, ICryptographyService crypto)
        {
            return crypto.Verify(originalContent, Value, publicKey);
        }

        /// <summary>
        /// Obtiene la firma en formato Base64 para serialización
        /// </summary>
        public string ToBase64() => Convert.ToBase64String(Value);
    }
}

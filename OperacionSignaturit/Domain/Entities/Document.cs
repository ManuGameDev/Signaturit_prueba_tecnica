using Domain.Entities.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Representa un documento en el sistema con su contenido, hash e información de trazabilidad
    /// </summary>
    public class Document
    {
        public string Id { get; private set; }
        public string FileName { get; private set; }
        public byte[] Content { get; private set; }
        public string Hash { get; private set; }
        public DateTime DetectedAt { get; private set; }
        public string AgentId { get; private set; }
        public DocumentStatus Status { get; private set; }
        public Dictionary<string, string> Metadata { get; private set; }
        public List<Signature> Signatures { get; private set; }

        private Document()
        {
            Metadata = new Dictionary<string, string>();
            Signatures = new List<Signature>();
        }

        /// <summary>
        /// Verifica si el hash proporcionado coincide con el hash del documento
        /// </summary>
        public bool VerifyIntegrity(string providedHash)
        {
            return Hash.Equals(providedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Marca el documento como validado
        /// </summary>
        public void MarkAsValidated()
        {
            if (Status != DocumentStatus.PENDING)
                throw new InvalidOperationException($"Cannot validate document in status {Status}");

            Status = DocumentStatus.VALIDATED;
        }

        /// <summary>
        /// Marca el documento como firmado y asocia la firma
        /// </summary>
        public void MarkAsSigned(Signature signature)
        {
            if (Status != DocumentStatus.VALIDATED)
                throw new InvalidOperationException("Only validated documents can be signed");

            Signatures.Add(signature);
            Status = DocumentStatus.SIGNED;
        }

        /// <summary>
        /// Marca el documento como rechazado con una razón
        /// </summary>
        public void Reject(string reason)
        {
            Status = DocumentStatus.REJECTED;
            Metadata["rejection_reason"] = reason;
        }

        /// <summary>
        /// Obtiene el tamaño del documento en bytes
        /// </summary>
        public long GetSizeInBytes() => Content?.Length ?? 0;

        /// <summary>
        /// Factory method interno para el Builder
        /// </summary>
        internal static Document CreateInternal(
            string id,
            string fileName,
            byte[] content,
            string hash,
            string agentId,
            Dictionary<string, string> metadata)
        {
            return new Document
            {
                Id = id,
                FileName = fileName,
                Content = content,
                Hash = hash,
                DetectedAt = DateTime.UtcNow,
                AgentId = agentId,
                Status = DocumentStatus.PENDING,
                Metadata = metadata ?? new Dictionary<string, string>()
            };
        }
    }
}

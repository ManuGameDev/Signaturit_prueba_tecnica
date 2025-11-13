using Domain.Entities.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Entities
{
    /// <summary>
    /// Representa una entrada en el log de auditoría con encadenamiento inmutable
    /// </summary>
    public class AuditEntry
    {
        public long Id { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string AgentId { get; private set; }
        public string DocumentId { get; private set; }
        public AuditAction Action { get; private set; }
        public AuditResult Result { get; private set; }
        public string Details { get; private set; }
        public string PreviousEntryHash { get; private set; }
        public string CurrentHash { get; private set; }

        private AuditEntry() { }

        /// <summary>
        /// Crea una nueva entrada de auditoría con hash encadenado
        /// </summary>
        public static AuditEntry Create(
            string agentId,
            string documentId,
            AuditAction action,
            AuditResult result,
            string details,
            string previousHash)
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                AgentId = agentId ?? "SYSTEM",
                DocumentId = documentId,
                Action = action,
                Result = result,
                Details = details,
                PreviousEntryHash = previousHash ?? string.Empty
            };

            // Calcula el hash de esta entrada
            entry.CurrentHash = entry.ComputeHash();

            return entry;
        }

        /// <summary>
        /// Calcula el hash SHA256 de esta entrada (blockchain-like)
        /// </summary>
        public string ComputeHash()
        {
            var data = $"{Timestamp:O}|{AgentId}|{DocumentId}|{Action}|{Result}|{Details}|{PreviousEntryHash}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verifica que el hash actual sea correcto
        /// </summary>
        public bool VerifyHash()
        {
            return CurrentHash == ComputeHash();
        }

        /// <summary>
        /// Establece el ID (usado por el repositorio después de guardar)
        /// </summary>
        public void SetId(long id)
        {
            Id = id;
        }
    }
}

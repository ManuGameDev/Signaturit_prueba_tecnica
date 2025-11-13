using Domain.Ports;

namespace Domain.Entities
{
    /// <summary>
    /// Representa un agente del sistema con su identidad criptográfica
    /// </summary>
    public class Agent
    {
        public string Id { get; private set; }
        public string PublicKey { get; private set; }
        public bool IsTrusted { get; private set; }
        public DateTime RegisteredAt { get; private set; }
        public int RejectionCount { get; private set; }
        public DateTime? LastActivityAt { get; private set; }

        private Agent() { }

        /// <summary>
        /// Marca el agente como sospechoso incrementando su contador de rechazos
        /// </summary>
        public void MarkAsSuspicious()
        {
            RejectionCount++;
            if (RejectionCount >= 3)
            {
                IsTrusted = false;
            }
        }

        /// <summary>
        /// Resetea el contador de rechazos (si se rehabilita el agente)
        /// </summary>
        public void ResetRejections()
        {
            RejectionCount = 0;
            IsTrusted = true;
        }

        /// <summary>
        /// Actualiza el timestamp de última actividad
        /// </summary>
        public void UpdateActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica una firma usando la clave pública del agente
        /// </summary>
        public bool VerifySignature(byte[] data, byte[] signature, ICryptographyService crypto)
        {
            return crypto.Verify(data, signature, PublicKey);
        }

        /// <summary>
        /// Factory method interno para AgentFactory
        /// </summary>
        internal static Agent CreateInternal(string id, string publicKey)
        {
            return new Agent
            {
                Id = id,
                PublicKey = publicKey,
                IsTrusted = true,
                RegisteredAt = DateTime.UtcNow,
                RejectionCount = 0
            };
        }
    }
}

using Domain.Entities;
using Domain.Ports;

namespace Domain.Patterns
{

    /// <summary>
    /// Factory para crear agentes con sus pares de claves criptográficas
    /// </summary>
    public class AgentFactory
    {
        private readonly ICryptographyService _cryptoService;

        public AgentFactory() { }

        public AgentFactory(ICryptographyService cryptoService)
        {
            _cryptoService = cryptoService ??
                throw new ArgumentNullException(nameof(cryptoService));
        }

        /// <summary>
        /// Crea un nuevo agente generando un par de claves RSA
        /// </summary>
        /// <returns>Tupla con el agente (con clave pública) y su clave privada</returns>
        public (Agent agent, string privateKey) CreateAgent(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));

            var (publicKey, privateKey) = _cryptoService.GenerateKeyPair();
            var agent = Agent.CreateInternal(agentId, publicKey);

            return (agent, privateKey);
        }

        /// <summary>
        /// Crea un agente importando una clave pública existente
        /// </summary>
        public Agent CreateAgentFromPublicKey(string agentId, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));

            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Public key cannot be empty", nameof(publicKey));

            return Agent.CreateInternal(agentId, publicKey);
        }
    }
}
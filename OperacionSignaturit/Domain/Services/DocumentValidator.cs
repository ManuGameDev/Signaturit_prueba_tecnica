using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Entities.Results;
using Domain.Ports;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Services
{

    /// <summary>
    /// Servicio de dominio para validación completa de documentos
    /// </summary>
    public class DocumentValidator
    {
        private readonly ICryptographyService _cryptoService;
        private readonly IAgentRepository _agentRepository;
        private readonly IAuditLog _auditLog;

        public DocumentValidator(
            ICryptographyService cryptoService,
            IAgentRepository agentRepository,
            IAuditLog auditLog)
        {
            _cryptoService = cryptoService ??
                throw new ArgumentNullException(nameof(cryptoService));
            _agentRepository = agentRepository ??
                throw new ArgumentNullException(nameof(agentRepository));
            _auditLog = auditLog ??
                throw new ArgumentNullException(nameof(auditLog));
        }

        /// <summary>
        /// Valida un documento: agente, firma e integridad
        /// </summary>
        public async Task<DocValidationResult> ValidateAsync(
            Document document,
            byte[] agentSignature)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (agentSignature == null || agentSignature.Length == 0)
                throw new ArgumentException("Agent signature cannot be empty", nameof(agentSignature));

            // 1. Verificar que el agente existe
            var agent = await _agentRepository.GetByIdAsync(document.AgentId);
            if (agent == null)
            {
                await LogAuditAsync(document, AuditAction.DOCUMENT_REJECTED,
                    AuditResult.FAILURE, "Agent not found");
                return DocValidationResult.Failure("Agent not found");
            }

            // 2. Verificar que el agente es confiable
            if (!agent.IsTrusted)
            {
                await LogAuditAsync(document, AuditAction.DOCUMENT_REJECTED,
                    AuditResult.FAILURE, "Agent not trusted");
                return DocValidationResult.Failure("Agent not trusted");
            }

            // 3. Verificar la firma del agente
            var dataToVerify = Encoding.UTF8.GetBytes(document.Hash);
            if (!_cryptoService.Verify(dataToVerify, agentSignature, agent.PublicKey))
            {
                agent.MarkAsSuspicious();
                await _agentRepository.UpdateAsync(agent);

                await LogAuditAsync(document, AuditAction.SIGNATURE_VERIFICATION_FAILED,
                    AuditResult.FAILURE, $"Invalid agent signature. Failures: {agent.RejectionCount}");

                return DocValidationResult.Failure("Invalid agent signature");
            }

            // 4. Verificar integridad del contenido
            var computedHash = _cryptoService.ComputeHash(document.Content);
            if (!document.VerifyIntegrity(computedHash))
            {
                await LogAuditAsync(document, AuditAction.INTEGRITY_CHECK_FAILED,
                    AuditResult.FAILURE, "Content integrity check failed");
                return DocValidationResult.Failure("Content integrity check failed");
            }

            // Todo OK - marcar como validado
            document.MarkAsValidated();
            agent.UpdateActivity();
            await _agentRepository.UpdateAsync(agent);

            await LogAuditAsync(document, AuditAction.DOCUMENT_VALIDATED,
                AuditResult.SUCCESS, "Document validated successfully");

            return DocValidationResult.Success();
        }

        private async Task LogAuditAsync(
            Document doc,
            AuditAction action,
            AuditResult result,
            string details)
        {
            var lastEntry = await _auditLog.GetLastEntryAsync();
            var entry = AuditEntry.Create(
                doc.AgentId,
                doc.Id,
                action,
                result,
                details,
                lastEntry?.CurrentHash);

            await _auditLog.LogAsync(entry);
        }
    }
}
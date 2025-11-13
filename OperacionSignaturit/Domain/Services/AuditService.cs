using Domain.Entities.Audit;
using Domain.Entities.Results;
using Domain.Ports;

namespace Domain.Services
{
    /// <summary>
    /// Servicio para operaciones avanzadas de auditoría
    /// </summary>
    public class AuditService
    {
        private readonly IAuditLog _auditLog;

        public AuditService(IAuditLog auditLog)
        {
            _auditLog = auditLog ??
                throw new ArgumentNullException(nameof(auditLog));
        }

        /// <summary>
        /// Verifica la integridad completa de la cadena de auditoría
        /// </summary>
        public async Task<AuditIntegrityResult> VerifyChainIntegrityAsync()
        {
            var isValid = await _auditLog.VerifyChainIntegrityAsync();

            if (!isValid)
            {
                return AuditIntegrityResult.Compromised(
                    "Audit chain integrity check failed");
            }

            return AuditIntegrityResult.Valid();
        }

        /// <summary>
        /// Genera un reporte de auditoría para un documento específico
        /// </summary>
        public async Task<DocumentAuditReport> GenerateDocumentReportAsync(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("Document ID cannot be empty", nameof(documentId));

            var entries = await _auditLog.GetEntriesAsync(documentId);
            return new DocumentAuditReport(documentId, entries);
        }

        /// <summary>
        /// Genera un reporte de auditoría para un agente específico
        /// </summary>
        public async Task<AgentAuditReport> GenerateAgentReportAsync(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));

            var entries = await _auditLog.GetEntriesByAgentAsync(agentId);
            return new AgentAuditReport(agentId, entries);
        }
    }
}
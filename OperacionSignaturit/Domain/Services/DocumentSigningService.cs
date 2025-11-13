using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Entities.Results;
using Domain.Ports;

namespace Domain.Services
{
    /// <summary>
    /// Servicio de dominio para el proceso de firmado de documentos
    /// </summary>
    public class DocumentSigningService
    {
        private readonly INotaryClient _notaryClient;
        private readonly IDocumentRepository _documentRepository;
        private readonly IAuditLog _auditLog;

        public DocumentSigningService(
            INotaryClient notaryClient,
            IDocumentRepository documentRepository,
            IAuditLog auditLog)
        {
            _notaryClient = notaryClient ??
                throw new ArgumentNullException(nameof(notaryClient));
            _documentRepository = documentRepository ??
                throw new ArgumentNullException(nameof(documentRepository));
            _auditLog = auditLog ??
                throw new ArgumentNullException(nameof(auditLog));
        }

        /// <summary>
        /// Firma un documento validado usando el servicio notario
        /// </summary>
        public async Task<SigningResult> SignDocumentAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document.Status != DocumentStatus.VALIDATED)
            {
                return SigningResult.Failure("Document must be validated before signing");
            }

            try
            {
                // Solicitar firma al notario
                var notaryResponse = await _notaryClient.SignDocumentAsync(
                    document.Id,
                    document.Content);

                // Crear entidad de firma
                var signature = Signature.Create(
                    document.Id,
                    Convert.FromBase64String(notaryResponse.Signature),
                    "NOTARY");

                // Marcar documento como firmado
                document.MarkAsSigned(signature);
                await _documentRepository.UpdateAsync(document);

                // Registrar en auditoría
                var lastEntry = await _auditLog.GetLastEntryAsync();
                var auditEntry = AuditEntry.Create(
                    document.AgentId,
                    document.Id,
                    AuditAction.DOCUMENT_SIGNED,
                    AuditResult.SUCCESS,
                    $"Document signed by notary at {notaryResponse.Timestamp:O}",
                    lastEntry?.CurrentHash);

                await _auditLog.LogAsync(auditEntry);

                return SigningResult.Success(signature);
            }
            catch (Exception ex)
            {
                var lastEntry = await _auditLog.GetLastEntryAsync();
                var auditEntry = AuditEntry.Create(
                    document.AgentId,
                    document.Id,
                    AuditAction.DOCUMENT_REJECTED,
                    AuditResult.FAILURE,
                    $"Signing failed: {ex.Message}",
                    lastEntry?.CurrentHash);

                await _auditLog.LogAsync(auditEntry);

                return SigningResult.Failure($"Signing failed: {ex.Message}");
            }
        }
    }
}
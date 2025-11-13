namespace Domain.Entities.Enums
{
    public enum AuditAction
    {
        AGENT_CONNECTED,
        DOCUMENT_RECEIVED,
        DOCUMENT_VALIDATED,
        DOCUMENT_SIGNED,
        DOCUMENT_REJECTED,
        INTEGRITY_CHECK_FAILED,
        SIGNATURE_VERIFICATION_FAILED,
        AGENT_MARKED_SUSPICIOUS
    }
}

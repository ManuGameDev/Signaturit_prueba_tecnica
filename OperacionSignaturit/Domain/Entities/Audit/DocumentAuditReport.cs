namespace Domain.Entities.Audit
{
    public class DocumentAuditReport
    {
        public string DocumentId { get; }
        public IEnumerable<AuditEntry> Entries { get; }

        public DocumentAuditReport(string documentId, IEnumerable<AuditEntry> entries)
        {
            DocumentId = documentId;
            Entries = entries ?? new List<AuditEntry>();
        }
    }
}

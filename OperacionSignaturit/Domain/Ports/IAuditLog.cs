using Domain.Entities;

namespace Domain.Ports
{
    /// <summary>
    /// Puerto para log de auditoría inmutable
    /// </summary>
    public interface IAuditLog
    {
        Task LogAsync(AuditEntry entry);
        Task<IEnumerable<AuditEntry>> GetEntriesAsync(string documentId);
        Task<IEnumerable<AuditEntry>> GetAllEntriesAsync();
        Task<IEnumerable<AuditEntry>> GetEntriesByAgentAsync(string agentId);
        Task<AuditEntry> GetLastEntryAsync();
        Task<bool> VerifyChainIntegrityAsync();
    }
}
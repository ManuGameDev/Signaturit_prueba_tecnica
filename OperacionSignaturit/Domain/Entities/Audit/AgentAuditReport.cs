namespace Domain.Entities.Audit
{
    public class AgentAuditReport
    {
        public string AgentId { get; }
        public IEnumerable<AuditEntry> Entries { get; }

        public AgentAuditReport(string agentId, IEnumerable<AuditEntry> entries)
        {
            AgentId = agentId;
            Entries = entries ?? new List<AuditEntry>();
        }
    }
}

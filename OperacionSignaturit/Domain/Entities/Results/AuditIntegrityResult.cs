namespace Domain.Entities.Results
{
    public class AuditIntegrityResult
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; }

        private AuditIntegrityResult() { }

        public static AuditIntegrityResult Valid() => new()
        {
            IsValid = true,
            Message = "Audit chain integrity verified"
        };

        public static AuditIntegrityResult Compromised(string message) => new()
        {
            IsValid = false,
            Message = message
        };
    }
}
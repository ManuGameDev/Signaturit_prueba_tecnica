namespace Domain.Ports
{
    /// <summary>
    /// Puerto para cliente del servicio notario (HTTP)
    /// </summary>
    public interface INotaryClient
    {
        Task<NotarySignResponse> SignDocumentAsync(string documentId, byte[] content);
        Task<bool> HealthCheckAsync();
    }

    public class NotarySignResponse
    {
        public string Signature { get; set; }
        public DateTime Timestamp { get; set; }
        public string DocumentId { get; set; }
    }
}

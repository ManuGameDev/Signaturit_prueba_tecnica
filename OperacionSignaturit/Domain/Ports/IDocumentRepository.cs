using Domain.Entities;

namespace Domain.Ports
{
    /// <summary>
    /// Puerto para repositorio de documentos
    /// </summary>
    public interface IDocumentRepository
    {
        Task SaveAsync(Document document);
        Task<Document> GetByIdAsync(string documentId);
        Task<IEnumerable<Document>> GetByAgentAsync(string agentId);
        Task UpdateAsync(Document document);
    }
}

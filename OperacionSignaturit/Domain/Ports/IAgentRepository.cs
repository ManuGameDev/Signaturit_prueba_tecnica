using Domain.Entities;

namespace Domain.Ports
{
    /// <summary>
    /// Puerto para repositorio de agentes
    /// </summary>
    public interface IAgentRepository
    {
        Task SaveAsync(Agent agent);
        Task<Agent> GetByIdAsync(string agentId);
        Task<IEnumerable<Agent>> GetAllAsync();
        Task<IEnumerable<Agent>> GetAllTrustedAsync();
        Task UpdateAsync(Agent agent);
        Task<bool> ExistsAsync(string agentId);
        Task DeleteAsync(string agentId);
    }
}
using Microsoft.Data.Sqlite;
using Domain.Entities;
using Domain.Ports;
using Domain.Patterns;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Repositorio de agentes con persistencia SQLite
    /// </summary>
    public class SqliteAgentRepository : IAgentRepository, IDisposable
    {
        private readonly string _connectionString;
        private readonly AgentFactory _agentFactory;

        public SqliteAgentRepository(string databasePath)
        {
            _agentFactory = new AgentFactory();

            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be empty", nameof(databasePath));

            _connectionString = $"Data Source={databasePath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Agents (
                    Id TEXT PRIMARY KEY,
                    PublicKey TEXT NOT NULL,
                    IsTrusted INTEGER NOT NULL,
                    RegisteredAt TEXT NOT NULL,
                    RejectionCount INTEGER NOT NULL,
                    LastActivityAt TEXT
                )";
            command.ExecuteNonQuery();
        }

        public async Task SaveAsync(Agent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Agents (Id, PublicKey, IsTrusted, RegisteredAt, RejectionCount, LastActivityAt)
                VALUES (@Id, @PublicKey, @IsTrusted, @RegisteredAt, @RejectionCount, @LastActivityAt)";

            command.Parameters.AddWithValue("@Id", agent.Id);
            command.Parameters.AddWithValue("@PublicKey", agent.PublicKey);
            command.Parameters.AddWithValue("@IsTrusted", agent.IsTrusted ? 1 : 0);
            command.Parameters.AddWithValue("@RegisteredAt", agent.RegisteredAt.ToString("O"));
            command.Parameters.AddWithValue("@RejectionCount", agent.RejectionCount);
            command.Parameters.AddWithValue("@LastActivityAt", agent.LastActivityAt?.ToString("O") ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Agent> GetByIdAsync(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                return null;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Agents WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", agentId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToAgent(reader);
            }

            return null;
        }

        public async Task<IEnumerable<Agent>> GetAllAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Agents";

            var agents = new List<Agent>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                agents.Add(MapToAgent(reader));
            }

            return agents;
        }

        public async Task<IEnumerable<Agent>> GetAllTrustedAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Agents WHERE IsTrusted = 1";

            var agents = new List<Agent>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                agents.Add(MapToAgent(reader));
            }

            return agents;
        }

        public async Task UpdateAsync(Agent agent)
        {
            await SaveAsync(agent); // INSERT OR REPLACE maneja updates
        }

        public async Task<bool> ExistsAsync(string agentId)
        {
            var agent = await GetByIdAsync(agentId);
            return agent != null;
        }

        public async Task DeleteAsync(string agentId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Agents WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", agentId);

            await command.ExecuteNonQueryAsync();
        }

        private Agent MapToAgent(SqliteDataReader reader)
        {
            var publicKey = reader.GetString(reader.GetOrdinal("PublicKey"));
            var agent = _agentFactory.CreateAgentFromPublicKey(
                reader.GetString(reader.GetOrdinal("Id")),
                publicKey);

            // Usar reflection para setear campos privados (ya que son readonly)
            var isTrustedField = typeof(Agent).GetProperty("IsTrusted");
            isTrustedField?.SetValue(agent, reader.GetInt32(reader.GetOrdinal("IsTrusted")) == 1);

            var rejectionCountField = typeof(Agent).GetProperty("RejectionCount");
            rejectionCountField?.SetValue(agent, reader.GetInt32(reader.GetOrdinal("RejectionCount")));

            var lastActivityOrdinal = reader.GetOrdinal("LastActivityAt");
            if (!reader.IsDBNull(lastActivityOrdinal))
            {
                var lastActivityField = typeof(Agent).GetProperty("LastActivityAt");
                lastActivityField?.SetValue(agent, DateTime.Parse(reader.GetString(lastActivityOrdinal)));
            }

            return agent;
        }

        public void Dispose()
        {
            // Las conexiones Sqlite lo hacen automáticamente
        }
    }
}


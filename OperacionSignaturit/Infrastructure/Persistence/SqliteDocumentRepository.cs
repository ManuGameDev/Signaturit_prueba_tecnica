using Microsoft.Data.Sqlite;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Ports;
using System.Text.Json;
using Domain.Patterns;
using Infrastructure.Cryptography;

namespace SignaturitCore.Infrastructure.Persistence
{
    /// <summary>
    /// Repositorio de documentos con persistencia SQLite
    /// </summary>
    public class SqliteDocumentRepository : IDocumentRepository, IDisposable
    {
        private readonly string _connectionString;
        private readonly DocumentBuilder _documentBuilder;
        private readonly ICryptographyService _cryptographyService;

        public SqliteDocumentRepository(string databasePath)
        {
            _documentBuilder = new DocumentBuilder();
            _cryptographyService = new RsaCryptographyService();

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
                CREATE TABLE IF NOT EXISTS Documents (
                    Id TEXT PRIMARY KEY,
                    FileName TEXT NOT NULL,
                    Content BLOB NOT NULL,
                    Hash TEXT NOT NULL,
                    DetectedAt TEXT NOT NULL,
                    AgentId TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    Metadata TEXT,
                    Signatures TEXT
                )";
            command.ExecuteNonQuery();
        }

        public async Task SaveAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Documents 
                (Id, FileName, Content, Hash, DetectedAt, AgentId, Status, Metadata, Signatures)
                VALUES (@Id, @FileName, @Content, @Hash, @DetectedAt, @AgentId, @Status, @Metadata, @Signatures)";

            command.Parameters.AddWithValue("@Id", document.Id);
            command.Parameters.AddWithValue("@FileName", document.FileName);
            command.Parameters.AddWithValue("@Content", document.Content);
            command.Parameters.AddWithValue("@Hash", document.Hash);
            command.Parameters.AddWithValue("@DetectedAt", document.DetectedAt.ToString("O"));
            command.Parameters.AddWithValue("@AgentId", document.AgentId);
            command.Parameters.AddWithValue("@Status", (int)document.Status);
            command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(document.Metadata));
            command.Parameters.AddWithValue("@Signatures", JsonSerializer.Serialize(
                document.Signatures.Select(s => s.ToBase64()).ToList()));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Document> GetByIdAsync(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return null;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Documents WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", documentId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDocument(reader);
            }

            return null;
        }

        public async Task<IEnumerable<Document>> GetByAgentAsync(string agentId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Documents WHERE AgentId = @AgentId";
            command.Parameters.AddWithValue("@AgentId", agentId);

            var documents = new List<Document>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(MapToDocument(reader));
            }

            return documents;
        }

        public async Task<IEnumerable<Document>> GetByStatusAsync(DocumentStatus status)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Documents WHERE Status = @Status";
            command.Parameters.AddWithValue("@Status", (int)status);

            var documents = new List<Document>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(MapToDocument(reader));
            }

            return documents;
        }

        public async Task UpdateAsync(Document document)
        {
            await SaveAsync(document);
        }

        public async Task<int> CountAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Documents";

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private Document MapToDocument(SqliteDataReader reader)
        {
            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
                reader.GetString(reader.GetOrdinal("Metadata"))) ?? new Dictionary<string, string>();

            _documentBuilder.WithFileName(reader.GetString(reader.GetOrdinal("FileName")));
            _documentBuilder.WithContent((byte[])reader["Content"]);
            _documentBuilder.FromAgent(reader.GetString(reader.GetOrdinal("AgentId")));
            _documentBuilder.AddMetadata(metadata);

            Document document = _documentBuilder.Build(_cryptographyService);

            // Set status
            var statusField = typeof(Document).GetProperty("Status");
            statusField?.SetValue(document, (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")));

            return document;
        }

        public void Dispose()
        {
            // Las conexiones Sqlite lo hacen automáticamente
        }
    }
}

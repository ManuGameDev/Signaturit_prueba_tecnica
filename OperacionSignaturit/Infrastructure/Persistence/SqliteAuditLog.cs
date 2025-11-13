using Microsoft.Data.Sqlite;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Ports;

namespace SignaturitCore.Infrastructure.Persistence
{
    /// <summary>
    /// Log de auditoría inmutable con persistencia SQLite
    /// </summary>
    public class SqliteAuditLog : IAuditLog, IDisposable
    {
        private readonly string _connectionString;

        public SqliteAuditLog(string databasePath)
        {
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
                CREATE TABLE IF NOT EXISTS AuditLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    AgentId TEXT NOT NULL,
                    DocumentId TEXT NOT NULL,
                    Action INTEGER NOT NULL,
                    Result INTEGER NOT NULL,
                    Details TEXT,
                    PreviousEntryHash TEXT NOT NULL,
                    CurrentHash TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        public async Task LogAsync(AuditEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO AuditLog (Timestamp, AgentId, DocumentId, Action, Result, Details, PreviousEntryHash, CurrentHash)
                VALUES (@Timestamp, @AgentId, @DocumentId, @Action, @Result, @Details, @PreviousEntryHash, @CurrentHash);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@Timestamp", entry.Timestamp.ToString("O"));
            command.Parameters.AddWithValue("@AgentId", entry.AgentId);
            command.Parameters.AddWithValue("@DocumentId", entry.DocumentId);
            command.Parameters.AddWithValue("@Action", (int)entry.Action);
            command.Parameters.AddWithValue("@Result", (int)entry.Result);
            command.Parameters.AddWithValue("@Details", entry.Details ?? string.Empty);
            command.Parameters.AddWithValue("@PreviousEntryHash", entry.PreviousEntryHash ?? string.Empty);
            command.Parameters.AddWithValue("@CurrentHash", entry.CurrentHash);

            var id = Convert.ToInt64(await command.ExecuteScalarAsync());
            entry.SetId(id);
        }

        public async Task<IEnumerable<AuditEntry>> GetEntriesAsync(string documentId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AuditLog WHERE DocumentId = @DocumentId ORDER BY Id";
            command.Parameters.AddWithValue("@DocumentId", documentId);

            var entries = new List<AuditEntry>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(MapToAuditEntry(reader));
            }

            return entries;
        }

        public async Task<IEnumerable<AuditEntry>> GetAllEntriesAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AuditLog ORDER BY Id";

            var entries = new List<AuditEntry>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(MapToAuditEntry(reader));
            }

            return entries;
        }

        public async Task<IEnumerable<AuditEntry>> GetEntriesByAgentAsync(string agentId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AuditLog WHERE AgentId = @AgentId ORDER BY Id";
            command.Parameters.AddWithValue("@AgentId", agentId);

            var entries = new List<AuditEntry>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(MapToAuditEntry(reader));
            }

            return entries;
        }

        public async Task<AuditEntry> GetLastEntryAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AuditLog ORDER BY Id DESC LIMIT 1";

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToAuditEntry(reader);
            }

            return null;
        }

        public async Task<bool> VerifyChainIntegrityAsync()
        {
            var entries = await GetAllEntriesAsync();
            var entryList = entries.ToList();

            if (entryList.Count == 0)
                return true;

            // Verificar primera entrada
            if (entryList[0].PreviousEntryHash != string.Empty)
                return false;

            // Verificar cadena
            for (int i = 1; i < entryList.Count; i++)
            {
                var current = entryList[i];
                var previous = entryList[i - 1];

                if (current.PreviousEntryHash != previous.CurrentHash)
                    return false;

                if (!current.VerifyHash())
                    return false;
            }

            return true;
        }

        private AuditEntry MapToAuditEntry(SqliteDataReader reader)
        {
            var entry = AuditEntry.Create(
                reader.GetString(reader.GetOrdinal("AgentId")),
                reader.GetString(reader.GetOrdinal("DocumentId")),
                (AuditAction)reader.GetInt32(reader.GetOrdinal("Action")),
                (AuditResult)reader.GetInt32(reader.GetOrdinal("Result")),
                reader.GetString(reader.GetOrdinal("Details")),
                reader.GetString(reader.GetOrdinal("PreviousEntryHash")));

            entry.SetId(reader.GetInt64(reader.GetOrdinal("Id")));
            return entry;
        }

        public void Dispose()
        {
            // Las conexiones Sqlite lo hacen automáticamente
        }
    }
}

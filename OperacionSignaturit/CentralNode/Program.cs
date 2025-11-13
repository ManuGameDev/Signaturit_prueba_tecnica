using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Entities.Results;
using Domain.Patterns;
using Domain.Services;
using Infrastructure.Cryptography;
using Infrastructure.Persistence;
using SignaturitCore.Infrastructure.Persistence;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

Console.Clear();
Console.WriteLine("╔═══════════════════════════════════════╗");
Console.WriteLine("║   SIGNATURIT CENTRAL NODE - FASE 2    ║");
Console.WriteLine("╚═══════════════════════════════════════╝");
Console.WriteLine();

// Configurar servicios con Dependency Injection manual
var cryptoService = new RsaCryptographyService();
var agentRepo = new SqliteAgentRepository("agents.db");
var documentRepo = new SqliteDocumentRepository("documents.db");
var auditLog = new SqliteAuditLog("audit.db");

var agentFactory = new AgentFactory(cryptoService);
var documentValidator = new DocumentValidator(cryptoService, agentRepo, auditLog);

//Inicializar bundle de SQLite
SQLitePCL.Batteries_V2.Init();

// Registrar agentes
Console.WriteLine("[SETUP] Configurando agentes...\n");

if (File.Exists("AGENT-001.key"))
{
    try
    {
        var privateKey = File.ReadAllText("AGENT-001.key");
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);
        var publicKey = rsa.ExportRSAPublicKeyPem();

        var agent = agentFactory.CreateAgentFromPublicKey("AGENT-001", publicKey);
        await agentRepo.SaveAsync(agent);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SETUP] Agente registrado: AGENT-001");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("No se encontró AGENT-001.key");
    Console.ResetColor();

    var (agent, priv) = agentFactory.CreateAgent("AGENT-001");
    await agentRepo.SaveAsync(agent);
    File.WriteAllText("AGENT-001.key", priv);
    Console.WriteLine("Agente de prueba generado\n");
}

Console.WriteLine();

// Configuración
var port = args.Length > 0 ? int.Parse(args[0]) : 5000;
var notaryUrl = args.Length > 1 ? args[1] : "http://localhost:5001";

// Estadísticas al salir
Console.CancelKeyPress += async (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n\n Deteniendo servidor...");

    // Mostrar documentos
    var docs = await documentRepo.GetByStatusAsync(DocumentStatus.SIGNED);
    Console.WriteLine($"\nDocumentos firmados: {System.Linq.Enumerable.Count(docs)}");

    // Verificar integridad del audit log
    var isValid = await auditLog.VerifyChainIntegrityAsync();
    Console.WriteLine($"Integridad de auditoría: {(isValid ? "VÁLIDA" : "COMPROMETIDA")}");

    Environment.Exit(0);
};

// Iniciar servidor TCP
var listener = new TcpListener(IPAddress.Any, port);
listener.Start();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"[CENTRAL] Escuchando en puerto {port}");
Console.WriteLine($"[CENTRAL] Notario: {notaryUrl}");
Console.ResetColor();
Console.WriteLine();

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClientAsync(
        client,
        documentValidator,
        documentRepo,
        auditLog,
        notaryUrl,
        cryptoService));
}

// Handler de clientes
static async Task HandleClientAsync(
    TcpClient client,
    DocumentValidator validator,
    SqliteDocumentRepository docRepo,
    SqliteAuditLog audit,
    string notaryUrl,
    RsaCryptographyService crypto)
{
    try
    {
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var json = await reader.ReadLineAsync();

        var message = JsonSerializer.Deserialize<JsonElement>(json);

        Console.WriteLine($"[CENTRAL] Mensaje de {message.GetProperty("AgentId").GetString()}");

        // Reconstruir documento
        var builder = new DocumentBuilder();
        var document = builder
            .WithFileName(message.GetProperty("FileName").GetString())
            .WithContent(Convert.FromBase64String(message.GetProperty("ContentBase64").GetString()))
            .FromAgent(message.GetProperty("AgentId").GetString())
            .Build(crypto);

        var signature = Convert.FromBase64String(message.GetProperty("AgentSignatureBase64").GetString());

        // Validar
        DocValidationResult validationResult = await validator.ValidateAsync(document, signature);

        if (!validationResult.IsValid)
        {
            var errorResponse = new { Success = false, Message = validationResult.ErrorMessage, DocumentId = document.Id };
            await SendJsonAsync(stream, errorResponse);
            return;
        }

        // Guardar documento
        await docRepo.SaveAsync(document);

        // Firmar con notario (simplificado)
        using var http = new HttpClient();
        var notaryRequest = new
        {
            DocumentId = document.Id,
            ContentBase64 = Convert.ToBase64String(document.Content)
        };

        var notaryJson = JsonSerializer.Serialize(notaryRequest);
        var content = new StringContent(notaryJson, Encoding.UTF8, "application/json");
        var notaryResponse = await http.PostAsync($"{notaryUrl}/sign", content);

        if (notaryResponse.IsSuccessStatusCode)
        {
            var notaryData = JsonSerializer.Deserialize<JsonElement>(
                await notaryResponse.Content.ReadAsStringAsync());

            var sig = Signature.Create(
                document.Id,
                Convert.FromBase64String(notaryData.GetProperty("Signature").GetString()),
                "NOTARY");

            document.MarkAsSigned(sig);
            await docRepo.UpdateAsync(document);

            // Auditar
            var lastEntry = await audit.GetLastEntryAsync();
            var auditEntry = AuditEntry.Create(
                document.AgentId,
                document.Id,
                AuditAction.DOCUMENT_SIGNED,
                AuditResult.SUCCESS,
                "Document signed by notary",
                lastEntry?.CurrentHash);
            await audit.LogAsync(auditEntry);

            var successResponse = new { Success = true, Message = "Document signed", DocumentId = document.Id };
            await SendJsonAsync(stream, successResponse);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[CENTRAL] ✗ Error: {ex.Message}");
        Console.ResetColor();
    }
    finally
    {
        client.Close();
    }
}

static async Task SendJsonAsync(NetworkStream stream, object obj)
{
    var json = JsonSerializer.Serialize(obj);
    var bytes = Encoding.UTF8.GetBytes(json + "\n");
    await stream.WriteAsync(bytes);
    await stream.FlushAsync();
}
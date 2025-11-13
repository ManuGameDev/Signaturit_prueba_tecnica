using Domain.Patterns;
using Domain.Ports;
using Infrastructure.Cryptography;
using System.Text;

Console.Clear();
Console.WriteLine("╔═══════════════════════════════════════╗");
Console.WriteLine("║     SIGNATURIT AGENT - FASE 2         ║");
Console.WriteLine("╚═══════════════════════════════════════╝");
Console.WriteLine();

var agentId = args.Length > 0 ? args[0] : "AGENT-001";
var watchFolder = args.Length > 1 ? args[1] : "./watch";
var centralHost = args.Length > 2 ? args[2] : "localhost";
var centralPort = args.Length > 3 ? int.Parse(args[3]) : 5000;

// Usar adaptadores
var cryptoService = new RsaCryptographyService();
var agentFactory = new AgentFactory(cryptoService);

//Inicializar bundle de SQLite
SQLitePCL.Batteries_V2.Init();

// Generar o cargar claves
string privateKey;
var keyFile = $"{agentId}.key";

if (File.Exists(keyFile))
{
    privateKey = File.ReadAllText(keyFile);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[{agentId}] ✓ Clave cargada desde {keyFile}");
    Console.ResetColor();
}
else
{
    var (agent, privKey) = agentFactory.CreateAgent(agentId);
    privateKey = privKey;
    File.WriteAllText(keyFile, privateKey);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[{agentId}] ⚠ Nueva clave generada: {keyFile}");
    Console.ResetColor();
    Console.WriteLine($"\n📋 CLAVE PÚBLICA:");
    Console.WriteLine("─────────────────────────────────────────");
    Console.WriteLine(agent.PublicKey);
    Console.WriteLine("─────────────────────────────────────────\n");
}

// Crear carpeta de vigilancia
Directory.CreateDirectory(watchFolder);

// FileWatcher (temporal - aún usamos FileSystemWatcher directo)
var watcher = new FileSystemWatcher(watchFolder)
{
    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
};

watcher.Created += async (s, e) =>
{
    await Task.Delay(200);
    await ProcessFileAsync(e.FullPath, agentId, privateKey, cryptoService, centralHost, centralPort);
};

watcher.EnableRaisingEvents = true;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"[{agentId}] 👁 Vigilando: {Path.GetFullPath(watchFolder)}");
Console.WriteLine($"[{agentId}] 🔗 Nodo Central: {centralHost}:{centralPort}");
Console.ResetColor();
Console.WriteLine();

await Task.Delay(-1);

// Helper para procesar archivos
static async Task ProcessFileAsync(
    string filePath,
    string agentId,
    string privateKey,
    ICryptographyService crypto,
    string centralHost,
    int centralPort)
{
    try
    {
        Console.WriteLine($"[{agentId}] 📄 Detectado: {Path.GetFileName(filePath)}");

        // Leer contenido
        var content = await File.ReadAllBytesAsync(filePath);

        // Usar DocumentBuilder
        var builder = new DocumentBuilder();
        var document = builder
            .WithFileName(Path.GetFileName(filePath))
            .WithContent(content)
            .FromAgent(agentId)
            .Build(crypto);

        // Firmar hash
        var hashBytes = Encoding.UTF8.GetBytes(document.Hash);
        var signature = crypto.Sign(hashBytes, privateKey);

        // Enviar por TCP (aún simplificado)
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(centralHost, centralPort);
        var stream = client.GetStream();

        var message = new
        {
            AgentId = agentId,
            DocumentId = document.Id,
            FileName = document.FileName,
            ContentBase64 = Convert.ToBase64String(content),
            Hash = document.Hash,
            AgentSignatureBase64 = Convert.ToBase64String(signature)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();

        // Recibir respuesta
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var responseJson = await reader.ReadLineAsync();
        var response = System.Text.Json.JsonSerializer.Deserialize<dynamic>(responseJson);

        if (response.GetProperty("Success").GetBoolean())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{agentId}] ✓ {response.GetProperty("Message").GetString()}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{agentId}] ✗ {response.GetProperty("Message").GetString()}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{agentId}] ✗ Error: {ex.Message}");
        Console.ResetColor();
    }
}
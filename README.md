# 🔐 Signaturit - Sistema de Firma Digital Distribuido

## 📋 Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Estructura del Proyecto](#estructura-del-proyecto)
4. [Componentes Detallados](#componentes-detallados)
5. [Flujo de Operación](#flujo-de-operación)
6. [Casos de Uso y Pruebas](#casos-de-uso-y-pruebas)

---

## 🎯 Resumen Ejecutivo

**Signaturit** es un sistema distribuido de firma digital que implementa una cadena de custodia completa para documentos sensibles. El sistema garantiza:

- ✅ **Autenticidad**: Cada agente está identificado criptográficamente
- ✅ **Integridad**: Los documentos no pueden ser alterados sin detección
- ✅ **Trazabilidad**: Cada acción queda registrada en un log inmutable
- ✅ **No repudio**: Las firmas digitales prueban el origen

---

## 🏗️ Arquitectura del Sistema

### Arquitectura Hexagonal (Ports & Adapters)

```
┌─────────────────────────────────────────────────┐
│                                                 │
│              SIGNATURIT.DOMAIN                  │
│           (Núcleo de Negocio)                   │
│                                                 │
│  ┌─────────────┐  ┌──────────────┐              │
│  │  Entities   │  │   Patterns   │              │
│  │  - Document │  │   - Factory  │              │
│  │  - Agent    │  │   - Builder  │              │
│  │  - Signature│  │   - Strategy │              │
│  │  - AuditLog │  │              │              │
│  └─────────────┘  └──────────────┘              │
│                                                 │
│  ┌──────────────────────────────────┐           │
│  │         Ports (Interfaces)       │           │
│  │  - ICryptographyService          │           │
│  │  - IDocumentRepository           │           │
│  │  - IAgentRepository              │           │
│  │  - IAuditLog                     │           │
│  │  - ITcpClient / ITcpServer       │           │
│  │  - IFileWatcher                  │           │
│  └──────────────────────────────────┘           │
└─────────────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌──────────────┐         ┌──────────────┐
│  ADAPTERS    │         │  ADAPTERS    │
│  (Infra)     │         │  (Infra)     │
│              │         │              │
│ - RsaCrypto  │         │ - TcpServer  │
│ - SqliteRepo │         │ - TcpClient  │
│ - FileWatcher│         │ - HttpClient │
└──────────────┘         └──────────────┘
```

### Componentes del Sistema

```
┌──────────────┐         TCP           ┌──────────────┐
│              │──────────────────────▶│              │
│   AGENTE     │                       │     NODO     │
│ (FileWatcher)│◀──────────────────────│   CENTRAL    │
└──────────────┘                       │  (Validator) │
                                       └──────┬───────┘
                                              │
                                              │ REST/HTTP
                                        ┌─────▼────────┐
                                        │   SERVICIO   │
                                        │   NOTARIO    │
                                        │  (Firma RSA) │
                                        └──────────────┘
                                       
                                       
                                       
                                       
```

---

## 📁 Estructura del Proyecto

```
Signaturit.Solution/
│
├── src/
│   ├── Signaturit.Domain/              # ⭐ Núcleo hexagonal
│   │   ├── Entities/
│   │   │   ├── Document.cs
│   │   │   ├── Agent.cs
│   │   │   ├── Signature.cs
│   │   │   └── AuditEntry.cs
│   │   ├── Ports/                      # Interfaces
│   │   │   ├── ICryptographyService.cs
│   │   │   ├── IDocumentRepository.cs
│   │   │   ├── IAgentRepository.cs
│   │   │   ├── IAuditLog.cs
│   │   │   └── ITcpClient.cs / ITcpServer.cs
│   │   ├── Patterns/
│   │   │   ├── AgentFactory.cs
│   │   │   ├── DocumentBuilder.cs
│   │   │   └── PriorityStrategies.cs
│   │   └── Services/
│   │       └── DocumentValidator.cs
│   │
│   ├── Signaturit.Infrastructure/       # Adaptadores
│   │   ├── Cryptography/
│   │   │   └── RsaCryptographyService.cs
│   │   ├── Persistence/
│   │   │   ├── SqliteDocumentRepository.cs
│   │   │   ├── SqliteAgentRepository.cs
│   │   │   └── SqliteAuditLog.cs
│   │   ├── Networking/
│   │   │   ├── TcpClientAdapter.cs
│   │   │   ├── TcpServerAdapter.cs
│   │   │   └── HttpNotaryClient.cs
│   │   └── FileSystem/
│   │       └── FileSystemWatcherAdapter.cs
│   │
│   ├── Signaturit.Agent/                # Aplicación agente
│   │   ├── Program.cs
│   │   ├── AgentConfiguration.cs
│   │   ├── appsettings.json
│   │   └── watch/                       # Carpeta vigilada
│   │
│   ├── Signaturit.CentralNode/          # Servidor TCP
│   │   ├── Program.cs
│   │   ├── MessageHandler.cs
│   │   ├── appsettings.json
│   │   └── data/                        # Base de datos
│   │
│   └── Signaturit.NotaryService/        # API REST
│       ├── Program.cs
│       ├── Controllers/
│       │   └── SignController.cs
│       ├── Models/
│       │   └── SignRequest.cs
│       └── appsettings.json
│
│
└── README.md
```

---

## 🔧 Componentes Detallados

### 1️⃣ **Agente Autónomo**

**Responsabilidad**: Vigilar archivos y enviarlos al nodo central.

**Flujo de operación**:

1. **Inicialización**:
   - Carga o genera su par de claves RSA
   - Se registra en el nodo central (envía su clave pública)
   - Inicia vigilancia de carpeta

2. **Detección de archivo**:
   ```csharp
   fileWatcher.FileDetected += async (s, e) =>
   {
       var content = File.ReadAllBytes(e.FilePath);
       var doc = new DocumentBuilder()
           .WithFileName(Path.GetFileName(e.FilePath))
           .WithContent(content)
           .FromAgent(agentId)
           .Build(cryptoService);
       
       // Firma el hash con su clave privada
       var hashBytes = Encoding.UTF8.GetBytes(doc.Hash);
       var signature = cryptoService.Sign(hashBytes, privateKey);
       
       // Empaqueta mensaje
       var message = new AgentMessage
       {
           AgentId = agentId,
           DocumentId = doc.Id,
           FileName = doc.FileName,
           ContentBase64 = Convert.ToBase64String(content),
           Hash = doc.Hash,
           SignatureBase64 = Convert.ToBase64String(signature),
           Timestamp = DateTime.UtcNow
       };
       
       // Envía por TCP
       await tcpClient.SendAsync(SerializeMessage(message));
   };
   ```

3. **Recepción de respuesta**:
   - Espera confirmación del nodo central
   - Registra resultado en log local

**Configuración** (`appsettings.json`):
```json
{
  "AgentId": "AGENT-001",
  "WatchFolder": "./watch",
  "CentralNode": {
    "Host": "localhost",
    "Port": 5000
  },
  "KeyStorage": "./keys/agent.key"
}
```

---

### 2️⃣ **Nodo Central**

**Responsabilidad**: Validar agentes, verificar integridad y orquestar firmado.

**Flujo de operación**:

1. **Escucha conexiones TCP**:
   ```csharp
   tcpServer.ClientConnected += async (s, e) =>
   {
       var connection = e.Connection;
       var data = await connection.ReceiveAsync();
       var message = DeserializeMessage<AgentMessage>(data);
       
       await ProcessMessageAsync(message, connection);
   };
   ```

2. **Validación multi-capa**:
   ```csharp
   async Task<bool> ValidateMessageAsync(AgentMessage msg)
   {
       // 1. Verificar agente existe y es confiable
       var agent = await agentRepo.GetByIdAsync(msg.AgentId);
       if (agent == null || !agent.IsTrusted)
           return false;
       
       // 2. Verificar firma del agente
       var hashBytes = Encoding.UTF8.GetBytes(msg.Hash);
       var signature = Convert.FromBase64String(msg.SignatureBase64);
       if (!agent.VerifySignature(hashBytes, signature, crypto))
       {
           agent.MarkAsSuspicious();
           return false;
       }
       
       // 3. Verificar integridad del contenido
       var content = Convert.FromBase64String(msg.ContentBase64);
       var computedHash = crypto.ComputeHash(content);
       if (computedHash != msg.Hash)
           return false;
       
       return true;
   }
   ```

3. **Envío al notario**:
   ```csharp
   var notaryResponse = await notaryClient.SignDocumentAsync(
       doc.Id, 
       doc.Content
   );
   
   var signature = Signature.Create(
       doc.Id,
       Convert.FromBase64String(notaryResponse.Signature),
       "NOTARY"
   );
   
   doc.MarkAsSigned(signature);
   await docRepo.UpdateAsync(doc);
   ```

4. **Auditoría inmutable**:
   ```csharp
   var lastEntry = await auditLog.GetLastEntryAsync();
   var entry = AuditEntry.Create(
       doc.AgentId,
       doc.Id,
       AuditAction.DOCUMENT_SIGNED,
       AuditResult.SUCCESS,
       $"Signed by notary at {DateTime.UtcNow}",
       lastEntry?.CurrentHash
   );
   await auditLog.LogAsync(entry);
   ```

---

### 3️⃣ **Servicio Notario (API REST)**

**Responsabilidad**: Firmar documentos validados con clave maestra.

**Endpoint principal**:

```csharp
[ApiController]
[Route("api")]
public class SignController : ControllerBase
{
    private readonly ICryptographyService _crypto;
    private readonly string _notaryPrivateKey;
    
    [HttpPost("sign")]
    public async Task<IActionResult> Sign([FromBody] SignRequest request)
    {
        try
        {
            // Decodificar contenido
            var content = Convert.FromBase64String(request.Content);
            
            // Calcular hash
            var hash = _crypto.ComputeHash(content);
            
            // Firmar hash
            var signature = _crypto.Sign(
                Encoding.UTF8.GetBytes(hash),
                _notaryPrivateKey
            );
            
            var response = new SignResponse
            {
                DocumentId = request.DocumentId,
                Signature = Convert.ToBase64String(signature),
                Timestamp = DateTime.UtcNow,
                Algorithm = "RSA-SHA256"
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

**Modelos**:

```csharp
public class SignRequest
{
    public string DocumentId { get; set; }
    public string Content { get; set; } // Base64
}

public class SignResponse
{
    public string DocumentId { get; set; }
    public string Signature { get; set; } // Base64
    public DateTime Timestamp { get; set; }
    public string Algorithm { get; set; }
}
```

---

## 🔄 Flujo de Operación Completo

### Escenario: Documento Legítimo

```
1. AGENTE detecta archivo "report.pdf"
   └─▶ Calcula SHA256: ABC123...
   └─▶ Firma con su clave privada: SIGN_AGENT_001

2. AGENTE envía por TCP al NODO CENTRAL:
   {
     "agentId": "AGENT-001",
     "fileName": "report.pdf",
     "content": "base64...",
     "hash": "ABC123...",
     "signature": "SIGN_AGENT_001"
   }

3. NODO CENTRAL valida:
   ✅ Agente existe en BD → OK
   ✅ Agente es confiable → OK
   ✅ Firma del agente válida → OK
   ✅ Hash coincide con contenido → OK

4. NODO CENTRAL envía a NOTARIO (REST):
   POST /api/sign
   {
     "documentId": "DOC-UUID",
     "content": "base64..."
   }

5. NOTARIO responde:
   {
     "signature": "SIGN_NOTARY",
     "timestamp": "2025-11-08T10:30:00Z"
   }

6. NODO CENTRAL registra en AUDIT LOG:
   Entry #5: {
     "action": "DOCUMENT_SIGNED",
     "result": "SUCCESS",
     "documentId": "DOC-UUID",
     "previousHash": "HASH_ENTRY_4"
   }

7. NODO CENTRAL responde al AGENTE:
   {
     "success": true,
     "message": "Document signed successfully"
   }
```

### Escenario: Contenido Alterado

```
1. AGENTE detecta archivo manipulado
   └─▶ Hash original: ABC123...
   └─▶ Contenido actual: XYZ789...

2. NODO CENTRAL valida:
   ✅ Agente OK
   ✅ Firma del agente OK
   ❌ Hash NO coincide → RECHAZO

3. NODO CENTRAL registra:
   Entry: {
     "action": "INTEGRITY_CHECK_FAILED",
     "result": "FAILURE",
     "details": "Hash mismatch"
   }

4. NODO CENTRAL responde al AGENTE:
   {
     "success": false,
     "message": "Integrity check failed"
   }
```

---

## 🧪 Casos de Uso y Pruebas

### Casos de Prueba Incluidos

| # | Escenario | Archivo | Resultado Esperado |
|---|-----------|---------|-------------------|
| 1 | Documento válido | `legit_report.txt` | ✅ Firmado |
| 2 | Firma agente inválida | `tampered_sig.txt` | ❌ Rechazado + agente sospechoso |
| 3 | Contenido alterado | `modified.txt` | ❌ Rechazado + alerta |
| 4 | Agente no autorizado | (agente nuevo) | ❌ Conexión denegada |
| 5 | Archivo muy grande | `large_file.zip` | ✅ Firmado (baja prioridad) |

## 🎓 Conceptos Clave Aplicados

1. **Arquitectura Hexagonal**: Núcleo puro + adaptadores intercambiables
2. **Factory Pattern**: Creación consistente de agentes con claves
3. **Builder Pattern**: Construcción flexible de documentos
4. **Strategy Pattern**: Algoritmos de priorización intercambiables
5. **Repository Pattern**: Abstracción de persistencia
6. **Chain of Responsibility** (implícito): Validaciones encadenadas
7. **Event-Driven**: FileWatcher con eventos

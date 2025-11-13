using Domain.Entities;
using Domain.Ports;

namespace Domain.Patterns
{

    /// <summary>
    /// Builder para construir documentos de forma fluida y segura
    /// </summary>
    public class DocumentBuilder
    {
        private string _fileName;
        private byte[] _content;
        private string _agentId;
        private Dictionary<string, string> _metadata = new();

        public DocumentBuilder WithFileName(string fileName)
        {
            _fileName = fileName;
            return this;
        }

        public DocumentBuilder WithContent(byte[] content)
        {
            _content = content;
            return this;
        }

        public DocumentBuilder FromAgent(string agentId)
        {
            _agentId = agentId;
            return this;
        }

        public DocumentBuilder AddMetadata(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _metadata[key] = value ?? string.Empty;
            }
            return this;
        }

        public DocumentBuilder AddMetadata(Dictionary<string,string> metadata)
        {
            if (metadata.Any())
            {
                _metadata = metadata;
            }
            return this;
        }

        /// <summary>
        /// Construye el documento calculando su hash automáticamente
        /// </summary>
        public Document Build(ICryptographyService cryptoService)
        {
            if (cryptoService == null)
                throw new ArgumentNullException(nameof(cryptoService));

            if (string.IsNullOrWhiteSpace(_fileName))
                throw new InvalidOperationException("FileName is required");

            if (_content == null || _content.Length == 0)
                throw new InvalidOperationException("Content is required");

            if (string.IsNullOrWhiteSpace(_agentId))
                throw new InvalidOperationException("AgentId is required");

            var hash = cryptoService.ComputeHash(_content);
            var id = Guid.NewGuid().ToString();

            return Document.CreateInternal(
                id,
                _fileName,
                _content,
                hash,
                _agentId,
                new Dictionary<string, string>(_metadata));
        }

        /// <summary>
        /// Resetea el builder para reutilizarlo
        /// </summary>
        public DocumentBuilder Reset()
        {
            _fileName = null;
            _content = null;
            _agentId = null;
            _metadata.Clear();
            return this;
        }
    }
}
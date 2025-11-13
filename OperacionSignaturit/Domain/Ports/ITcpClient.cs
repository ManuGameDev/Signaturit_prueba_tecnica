namespace Domain.Ports
{
    /// <summary>
    /// Puerto para cliente TCP (usado por Agente)
    /// </summary>
    public interface ITcpClient
    {
        Task ConnectAsync(string host, int port);
        Task SendAsync(byte[] data);
        Task<byte[]> ReceiveAsync();
        void Disconnect();
        bool IsConnected { get; }
    }
}

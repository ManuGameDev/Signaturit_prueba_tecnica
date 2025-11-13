namespace Domain.Ports
{
    /// <summary>
    /// Puerto para servidor TCP (usado por Nodo Central)
    /// </summary>
    public interface ITcpServer
    {
        event EventHandler<ClientConnectedEventArgs> ClientConnected;
        Task StartAsync(int port);
        Task StopAsync();
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public ITcpConnection Connection { get; set; }
    }

    /// <summary>
    /// Representa una conexión TCP individual
    /// </summary>
    public interface ITcpConnection
    {
        Task<byte[]> ReceiveAsync();
        Task SendAsync(byte[] data);
        void Close();
        string RemoteEndPoint { get; }
    }
}

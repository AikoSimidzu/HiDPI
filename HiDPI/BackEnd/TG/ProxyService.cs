namespace HiDPI.BackEnd.TG
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    public interface ITcpServerService
    {
        Task StartListeningAsync(CancellationToken cancellationToken);
    }

    public class TcpServerService : ITcpServerService
    {
        private readonly TGProxy _proxy;
        private TcpListener _listener;
        TcpListener listener = new TcpListener(IPAddress.Any, 1080);

        public TcpServerService()
        {
            _proxy = new TGProxy();
        }

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            listener.Start();
            var proxy = new TGProxy();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
                    _ = proxy.HandleClientAsync(client, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                //Console.WriteLine("Сервер остановлен.");
            }
            finally
            {
                //listener.Stop();
            }
        }
    }
}

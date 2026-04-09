namespace TGProxy.BackEnd.TG
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class WsPool
    {
        private readonly int _poolSize;

        private readonly ConcurrentDictionary<string, ConcurrentQueue<ClientWebSocket>> _pools = new();

        public WsPool(int poolSize = 3)
        {
            _poolSize = poolSize;
        }

        public async Task<ClientWebSocket> GetSocketAsync(int dcId, bool isMedia, string kwsDomain)
        {
            string key = $"{dcId}_{isMedia}";
            var queue = _pools.GetOrAdd(key, _ => new ConcurrentQueue<ClientWebSocket>());

            while (queue.TryDequeue(out var ws))
            {
                if (ws.State == WebSocketState.Open)
                {
                    _ = RefillPoolAsync(key, kwsDomain);
                    return ws;
                }
                else
                {
                    ws.Dispose();
                }
            }
            _ = RefillPoolAsync(key, kwsDomain);
            return await CreateSocketAsync(kwsDomain);
        }

        private async Task RefillPoolAsync(string key, string kwsDomain)
        {
            var queue = _pools[key];

            if (queue.Count >= _poolSize) return;

            try
            {
                var ws = await CreateSocketAsync(kwsDomain);
                if (ws.State == WebSocketState.Open)
                {
                    queue.Enqueue(ws);
                }
            }
            catch { }
        }

        private async Task<ClientWebSocket> CreateSocketAsync(string kwsDomain)
        {
            var ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("binary");
            ws.Options.SetRequestHeader("Origin", "https://web.telegram.org");
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            await ws.ConnectAsync(new Uri($"wss://{kwsDomain}/apiws"), CancellationToken.None).ConfigureAwait(false);
            return ws;
        }
    }
}
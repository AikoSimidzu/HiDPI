namespace TGProxy.BackEnd.TG
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TGProxy.BackEnd.TG.Crypto;

    public class TGProxy
    {
        private readonly byte[] _secret = Convert.FromHexString("00000000000000000000000000000000");
        private static readonly WsPool _pool = new WsPool(poolSize: 3);

        public async Task HandleClientAsync(TcpClient tcpClient, CancellationToken serverToken = default)
        {
            MtProtoHandshake parser = null;
            try
            {
                using var stream = tcpClient.GetStream();

                byte[] handshake = new byte[MtProtoHandshake.HandshakeLength];
                await ReadExactlyAsync(stream, handshake, handshake.Length, serverToken).ConfigureAwait(false);

                parser = new MtProtoHandshake();
                if (!parser.TryParse(handshake, _secret))
                {
                    return;
                }

                byte[] cltDecKey = ComputeSha256(parser.ClientPrekeyAndIv.AsSpan(0, 32), _secret);
                byte[] cltDecIv = parser.ClientPrekeyAndIv.AsSpan(32, 16).ToArray();

                var cltEncPrekeyIv = parser.ClientPrekeyAndIv.Reverse().ToArray();
                byte[] cltEncKey = ComputeSha256(cltEncPrekeyIv.AsSpan(0, 32), _secret);
                byte[] cltEncIv = cltEncPrekeyIv.AsSpan(32, 16).ToArray();

                using var cltDecryptor = new AesCtr(cltDecKey, cltDecIv);
                using var cltEncryptor = new AesCtr(cltEncKey, cltEncIv);
                cltDecryptor.Transform(new byte[64]);

                byte[] relayInit = parser.GenerateRelayInit((short)(parser.IsMedia ? -parser.DcId : parser.DcId));

                byte[] relayEncKey = relayInit.AsSpan(8, 32).ToArray();
                byte[] relayEncIv = relayInit.AsSpan(40, 16).ToArray();

                var relayDecPrekeyIv = relayInit.AsSpan(8, 48).ToArray().Reverse().ToArray();
                byte[] relayDecKey = relayDecPrekeyIv.AsSpan(0, 32).ToArray();
                byte[] relayDecIv = relayDecPrekeyIv.AsSpan(32, 16).ToArray();

                using var tgEncryptor = new AesCtr(relayEncKey, relayEncIv);
                using var tgDecryptor = new AesCtr(relayDecKey, relayDecIv);
                tgEncryptor.Transform(new byte[64]);

                string kwsDomain = GetKwsDomain(parser.DcId, parser.IsMedia);

                using var ws = await _pool.GetSocketAsync(parser.DcId, parser.IsMedia, kwsDomain).ConfigureAwait(false);

                await ws.SendAsync(relayInit, WebSocketMessageType.Binary, true, serverToken).ConfigureAwait(false);

                using var localCts = new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken, localCts.Token);

                var splitter = new MtProtoSplitter(relayInit, parser.ProtoInt);

                var t1 = PumpTcpToWs(stream, ws, cltDecryptor, tgEncryptor, splitter, linkedCts.Token);
                var t2 = PumpWsToTcp(ws, stream, tgDecryptor, cltEncryptor, linkedCts.Token);

                await Task.WhenAny(t1, t2).ConfigureAwait(false);

                localCts.Cancel();

                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                {
                    try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None).ConfigureAwait(false); } catch { }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                string dc = parser != null ? parser.DcId.ToString() : "?";
            }
            finally
            {
                tcpClient.Close();
            }
        }

        static byte[] ComputeSha256(ReadOnlySpan<byte> prekey, byte[] secret)
        {
            using var sha256 = SHA256.Create();
            byte[] buffer = new byte[prekey.Length + secret.Length];
            prekey.CopyTo(buffer);
            secret.CopyTo(buffer.AsSpan(prekey.Length));
            return sha256.ComputeHash(buffer);
        }

        static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer, offset, count - offset, ct).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("Клиент закрыл соединение");
                }
                offset += read;
            }
        }

        static string GetKwsDomain(int dcId, bool isMedia)
        {
            string mediaTag = isMedia ? "-1" : "";
            return $"kws{dcId}{mediaTag}.web.telegram.org";
        }

        static async Task PumpTcpToWs(NetworkStream tcp, ClientWebSocket ws, AesCtr cltDecryptor, AesCtr tgEncryptor, MtProtoSplitter splitter, CancellationToken ct)
        {
            var buf = new byte[8192];
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int read = await tcp.ReadAsync(buf, 0, buf.Length, ct);
                    if (read == 0 || ws.State != WebSocketState.Open) break;

                    var activeSpan = buf.AsSpan(0, read);
                    cltDecryptor.Transform(activeSpan);
                    tgEncryptor.Transform(activeSpan);

                    var parts = splitter.Split(activeSpan.ToArray());
                    foreach (var part in parts)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(part), WebSocketMessageType.Binary, true, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {

            }
        }

        static async Task PumpWsToTcp(ClientWebSocket ws, NetworkStream tcp, AesCtr tgDecryptor, AesCtr cltEncryptor, CancellationToken ct)
        {
            var buf = new byte[16384];
            try
            {
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct).ConfigureAwait(false);
                    if (res.MessageType == WebSocketMessageType.Close) break;

                    var activeSpan = buf.AsSpan(0, res.Count);
                    tgDecryptor.Transform(activeSpan);
                    cltEncryptor.Transform(activeSpan);

                    await tcp.WriteAsync(buf, 0, res.Count, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {

            }
        }
    }
}

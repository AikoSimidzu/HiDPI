namespace HiDPI.BackEnd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    internal class Network
    {
        public static async Task<string> GET_REQUEST(string URL)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                using (HttpClient hc = new HttpClient(handler))
                {
                    return await hc.GetStringAsync(new Uri(URL));
                }
            }
        }

        /// <summary>
        /// Проверка списка доменов (TCP)
        /// </summary>
        public async Task<List<DomainInfo>> CheckDomainsAsync(List<DomainInfo> domains, int timeout = 3000)
        {
            var tasks = domains.Select(async domain =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                if (domain.ByTCP)
                {
                    domain.BypassState = await CheckTcpAsync(domain.Url, domain.Keyword, timeout);
                }
                else
                {
                    domain.BypassState = await CheckUdpQuicAsync(domain.Url, timeout);
                }
                watch.Stop();

                domain.Ping = domain.IsSuccess ? watch.ElapsedMilliseconds : 0;

                return domain;
            });

            await Task.WhenAll(tasks);
            return domains;
        }

        /// <summary>
        /// Проверка доступности по UDP (QUIC/HTTP3)
        /// </summary>
        public async Task<BypassStatus> CheckUdpQuicAsync(string url, int timeoutMs = 3000)
        {
            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            try
            {
                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode ? BypassStatus.Success : BypassStatus.UnknownError;
            }
            catch (TaskCanceledException)
            {
                return BypassStatus.Timeout;
            }
            catch (HttpRequestException)
            {
                return BypassStatus.ProtocolError;
            }
            catch (Exception)
            {
                return BypassStatus.UnknownError;
            }
        }

        /// <summary>
        /// Проверка доступности по TCP
        /// </summary>
        public async Task<BypassStatus> CheckTcpAsync(string url, string expectedKeyword = null, int timeoutMs = 3000)
        {
            using var handler = new HttpClientHandler
            {
                // Проверяем сертификат
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                }
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

            try
            {
                var response = await client.GetAsync(url);

                string content = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(expectedKeyword) && !content.Contains(expectedKeyword))
                {
                    return BypassStatus.MitM;
                }

                return BypassStatus.Success;
            }
            catch (TaskCanceledException)
            {
                return BypassStatus.Timeout;
            }
            catch (HttpRequestException ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    if (socketEx.SocketErrorCode == SocketError.ConnectionReset)
                        return BypassStatus.ConnectionReset;

                    if (socketEx.SocketErrorCode == SocketError.HostNotFound)
                        return BypassStatus.DnsError;
                }

                return BypassStatus.ProtocolError;
            }
            catch (Exception)
            {
                return BypassStatus.UnknownError;
            }
        }
    }
}

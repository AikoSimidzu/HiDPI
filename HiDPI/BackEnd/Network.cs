namespace HiDPI.BackEnd
{
    using System;
    using System.Net.Http;
    using System.Net.NetworkInformation;

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

        public async Task<List<DomainInfo>> CheckDomainsAsync(List<DomainInfo> domains, int timeout = 1000)
        {
            var pingTasks = domains.Select(domain => PingSingleDomainAsync(domain, timeout));

            await Task.WhenAll(pingTasks);

            var sortedList = domains.OrderByDescending(d => d.IsSuccess).ThenBy(d => d.IsSuccess ? d.Ping : long.MaxValue).ToList();

            return sortedList;
        }

        /// <summary>
        /// Пингуем хост
        /// </summary>
        private async Task PingSingleDomainAsync(DomainInfo domain, int timeout)
        {
            if (string.IsNullOrWhiteSpace(domain.Url))
            {
                domain.Status = IPStatus.BadDestination;
                domain.Ping = 0;
                return;
            }

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(domain.Url, timeout);

                domain.Status = reply.Status;

                if (reply.Status == IPStatus.Success)
                {
                    domain.Ping = reply.RoundtripTime;
                }
                else
                {
                    domain.Ping = 0;
                }
            }
            catch (PingException)
            {
                domain.Status = IPStatus.DestinationHostUnreachable;
                domain.Ping = 0;
            }
            catch (Exception)
            {
                domain.Status = IPStatus.BadDestination;
                domain.Ping = 0;
            }
        }
    }
}

namespace HiDPI.BackEnd
{
    using System;
    using System.Net.Http;

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
    }
}

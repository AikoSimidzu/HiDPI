namespace HiDPI.BackEnd
{
    class InternalData
    {
        public static readonly string CurrentVersion = "1.2.1";

        public static readonly List<DomainInfo> Domains = new List<DomainInfo>()
        {
            new DomainInfo { Name = "CloudFlare ECH", Url = "https://cloudflare-ech.com", ByTCP = false},

            new DomainInfo { Name = "YouTube", Url = "https://youtube.com", ByTCP = false },
            new DomainInfo { Name = "YouTube Video", Url = "https://redirector.googlevideo.com/generate_204", ByTCP = false },

            new DomainInfo { Name = "Discord", Url = "https://discord.com", ByTCP = true, Keyword = "discord" },
            new DomainInfo { Name = "Discord CDN", Url = "https://discordcdn.com", ByTCP = true },
            new DomainInfo { Name = "Discord Attachments", Url = "https://discord-attachments-uploads-prd.storage.googleapis.com", ByTCP = true },
            new DomainInfo { Name = "Discord Storage (Files)", Url = "https://storage.googleapis.com", ByTCP = true },

            new DomainInfo { Name = "Telegram", Url = "https://telegram.org", ByTCP = true, Keyword = "telegram" },
            new DomainInfo { Name = "Telegram Web", Url = "https://web.telegram.org", ByTCP = true, Keyword = "telegram" },
            new DomainInfo { Name = "Telegram (t.me)", Url = "https://t.me", ByTCP = true, Keyword = "telegram" }
        };
    }
}

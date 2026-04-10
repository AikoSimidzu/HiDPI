namespace HiDPI.BackEnd
{
    class InternalData
    {
        public static readonly string CurrentVersion = "1.2.0";

        public static readonly List<DomainInfo> Domains = new List<DomainInfo>()
        {
            new DomainInfo { Name = "CloudFlare", Url = "1.1.1.1" },
            new DomainInfo { Name = "CloudFlare ECH", Url = "cloudflare-ech.com"},
            new DomainInfo { Name = "Google", Url = "8.8.8.8" },
            new DomainInfo { Name = "Yandex", Url = "77.88.8.8" },
            new DomainInfo { Name = "YouTube", Url = "youtube.com" },
            new DomainInfo { Name = "Discord", Url = "discord.gg" },
            new DomainInfo { Name = "Discord CDN", Url = "discordcdn.com" },
            new DomainInfo { Name = "Discord Attachments", Url = "discord-attachments-uploads-prd.storage.googleapis.com" },
            new DomainInfo { Name = "Discord Stroage (Files)", Url = "storage.googleapis.com"},
            new DomainInfo { Name = "Discord Media", Url = "media.discordapp.net" },
            new DomainInfo { Name = "Discord Images", Url = "images-ext-1.discordapp.net" },
            new DomainInfo { Name = "Discord Gateway", Url = "gateway.discord.gg" },
            new DomainInfo { Name = "Telegram", Url = "telegram.org" },
            new DomainInfo { Name = "Telegram Web", Url = "web.telegram.org" },
            new DomainInfo { Name = "Telegram (t.me)", Url = "t.me" }
        };
    }
}

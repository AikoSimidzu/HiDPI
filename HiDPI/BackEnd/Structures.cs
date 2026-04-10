namespace HiDPI.BackEnd
{
    using System.Net.NetworkInformation;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement;

    public class DomainInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long Ping { get; set; } = 0;
        public IPStatus Status { get; set; } = IPStatus.Unknown;
        public bool IsSuccess => Status == IPStatus.Success;
        public bool IsTest { get; set; } = false;
    }

    public class LogMessage
    {
        public DateTime Time { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = "Info"; // Info, Error, System
    }

    public class ConfigInfo
    {
        public required string Name { get; set; }
        public required string ConfigPath { get; set; }
    }

    public class AppConfig
    {
        public bool StartUpWithSystem { get; set; } = false;
        public bool AutoConnect { get; set; } = false;
        public bool AutoRestartIfError { get; set; } = false;
        public bool AutoStartTGProxy { get; set; } = false;
        public ConfigInfo? LastConfig { get; set; }
    }
}

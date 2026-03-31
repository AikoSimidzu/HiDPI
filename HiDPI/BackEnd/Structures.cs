namespace HiDPI.BackEnd
{
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
        public ConfigInfo? LastConfig { get; set; }
    }
}

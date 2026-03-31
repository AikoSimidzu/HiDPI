namespace HiDPI.BackEnd
{
    using System;
    using System.IO;
    using System.Text.Json;

    internal class ConfigEngine
    {
        private string appCfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppConfig.cfg");
        public ConfigEngine() 
        { }

        public AppConfig LoadConfig()
        {
            AppConfig _appConfig = new() { AutoConnect = false, StartUpWithSystem = false, LastConfig = new ConfigInfo { Name = string.Empty, ConfigPath = string.Empty } };
            if (File.Exists(appCfgPath))
            {
                string config = File.ReadAllText(appCfgPath);
                if (config != null && config.Length > 0)
                {
                    _appConfig = JsonSerializer.Deserialize<AppConfig>(config);
                }
            }
            return _appConfig;
        }

        public void SetStartUpWithSystem(bool p, AppConfig config)
        {
            if (p)
            {
                Helper.RegisterTask();
            }
            else
            {
                Helper.DeleteTask();
            }

            config.StartUpWithSystem = p;
            Save(config);
        }

        public void SetAutoConnect(ConfigInfo path, bool p, AppConfig config)
        {
            config.AutoConnect = p;
            config.LastConfig = path;
            Save(config);
        }

        public void Save(AppConfig config)
        {
            File.WriteAllText(appCfgPath, JsonSerializer.Serialize(config));
        }
    }
}

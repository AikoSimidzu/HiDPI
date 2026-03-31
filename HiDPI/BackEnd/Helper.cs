namespace HiDPI.BackEnd
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Windows;
    using Microsoft.Win32.TaskScheduler;

    internal class Helper
    {
        public static async Task<string> GetActualVersion()
        {
            return await Network.GET_REQUEST("https://raw.githubusercontent.com/AikoSimidzu/HiDPI/refs/heads/main/CurrentVersion");
        }

        private static FileSystemWatcher _watcher;
        public static ObservableCollection<ConfigInfo> Configs { get; } = new ObservableCollection<ConfigInfo>();
        private static string cfgDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs");

        public static void InitializeConfigMonitoring()
        {
            if (!Directory.Exists(cfgDir)) Directory.CreateDirectory(cfgDir);

            RefreshConfigs();

            _watcher = new FileSystemWatcher(cfgDir)
            {
                Filter = "*.*",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _watcher.Created += (s, e) => UpdateUI();
            _watcher.Deleted += (s, e) => UpdateUI();
            _watcher.Renamed += (s, e) => UpdateUI();
        }

        private static void UpdateUI()
        {
            Application.Current.Dispatcher.Invoke(RefreshConfigs);
        }

        public static void RefreshConfigs()
        {
            var files = Directory.GetFiles(cfgDir);

            var currentFiles = Configs.Select(c => c.ConfigPath).ToList();

            for (int i = Configs.Count - 1; i >= 0; i--)
            {
                if (!files.Contains(Configs[i].ConfigPath))
                    Configs.RemoveAt(i);
            }

            foreach (var file in files)
            {
                if (!currentFiles.Contains(file))
                {
                    Configs.Add(new ConfigInfo
                    {
                        ConfigPath = file,
                        Name = Path.GetFileName(file)
                    });
                }
            }
        }

        public static void RegisterTask()
        {
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "HiDPI (Ani zapret2)";
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Triggers.Add(new LogonTrigger());
                td.Actions.Add(new ExecAction(AppDomain.CurrentDomain.BaseDirectory + "HiDPI.exe"));

                ts.RootFolder.RegisterTaskDefinition(@"HiDPIAutoStart", td);
            }
        }

        public static void DeleteTask()
        {
            using (TaskService ts = new TaskService())
            {
                ts.RootFolder.DeleteTask("HiDPIAutoStart", exceptionOnNotExists: false);
            }
        }
    }
}

namespace HiDPI.BackEnd
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    public class ZapretController : IDisposable
    {
        private Process? _winwsProcess;
        private FileSystemWatcher? _watcher;
        private DateTime _lastEventTime = DateTime.MinValue;

        private readonly string _baseDir;
        private readonly string _configPath;

        public event Action<string>? OnLogReceived;
        public event Action? OnProcessExited;
        public event Action? OnProcessRestarted;

        public bool IsRunning => _winwsProcess is { HasExited: false };

        public ZapretController(string ConfigPath)
        {
            _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zapret2");
            _configPath = ConfigPath;

            InitializeWatcher();
        }

        private void InitializeWatcher()
        {
            try
            {
                if (!Directory.Exists(_baseDir)) return;

                _watcher = new FileSystemWatcher(Path.GetDirectoryName(_configPath), Path.GetFileName(_configPath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnConfigChanged;
                _watcher.Created += OnConfigChanged;
                _watcher.Renamed += OnConfigChanged;
            }
            catch (Exception ex)
            {
                Log($"[ОШИБКА] Не удалось запустить FileSystemWatcher: {ex.Message}");
            }
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.Now - _lastEventTime).TotalMilliseconds < 500) return;
            _lastEventTime = DateTime.Now;

            OnProcessRestarted?.Invoke();
            Thread.Sleep(200);

            Restart();
        }

        public void Start()
        {
            if (IsRunning) return;

            string winwsPath = Path.Combine(_baseDir, "binaries", "winws2.exe");

            if (!File.Exists(winwsPath))
            {
                Log($"[ОШИБКА] Файл не найден: {winwsPath}");
                return;
            }

            string arguments = BuildArguments();
            if (string.IsNullOrEmpty(arguments))
            {
                Log("[ОШИБКА] Не удалось собрать аргументы. Проверь config.txt.");
                return;
            }

            Log($"[СТАРТ] Запуск winws с текущим конфигом...");

            var startInfo = new ProcessStartInfo
            {
                FileName = winwsPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _baseDir
            };

            try
            {
                _winwsProcess = new Process { StartInfo = startInfo };
                _winwsProcess.EnableRaisingEvents = true;

                _winwsProcess.OutputDataReceived += (s, e) => Log(e.Data);
                _winwsProcess.ErrorDataReceived += (s, e) => Log(e.Data);
                _winwsProcess.Exited += (s, e) =>
                {
                    var process = s as Process;
                    if (process != null)
                    {
                        try
                        {
                            Log($"[СИСТЕМА] winws завершён. Код: {_winwsProcess.ExitCode}");
                        }
                        catch { }
                        OnProcessExited?.Invoke();
                    }
                };

                _winwsProcess.Start();
                _winwsProcess.BeginOutputReadLine();
                _winwsProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log($"[ОШИБКА] {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_winwsProcess != null && !_winwsProcess.HasExited)
            {
                _winwsProcess.Kill();
                _winwsProcess.WaitForExit(650);
                _winwsProcess.Dispose();
                _winwsProcess = null;
                Log("[СИСТЕМА] Процесс остановлен.\n");
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private string BuildArguments()
        {
            if (!File.Exists(_configPath))
            {
                Log($"[ОШИБКА] Файл конфигурации не найден: {_configPath}");
                return string.Empty;
            }

            var sb = new StringBuilder();

            try
            {
                using var fs = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);

                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    string trimmed = line.Trim();

                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                        continue;

                    string processedLine = trimmed.Replace("{BASE_DIR}", _baseDir);
                    sb.Append(processedLine).Append(" ");
                }
            }
            catch (Exception ex)
            {
                Log($"[ОШИБКА чтении конфига] {ex.Message}");
                return string.Empty;
            }

            return sb.ToString().TrimEnd();
        }

        private void Log(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                OnLogReceived?.Invoke(text);
        }

        public void Dispose()
        {
            Stop();
            _watcher?.Dispose();
        }
    }
}

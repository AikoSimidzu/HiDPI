namespace HiDPI.MVVM.ViewModel
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using HiDPI.BackEnd;
    using HiDPI.BackEnd.TG;
    using HiDPI.MVVM.Core;

    class MainViewModel : ObservableObject
    {
        #region Бинды
        public ObservableCollection<LogMessage> Logs { get; } = new ObservableCollection<LogMessage>();
        public ObservableCollection<DomainInfo> PingLogs { get; } = new ObservableCollection<DomainInfo>();
        public ObservableCollection<ConfigInfo> Configs { get; set; }

        private readonly ITcpServerService _proxyService;
        private CancellationTokenSource _cancellationTokenSource;

        private string runnedConfig { get; set; }
        public string Status
        {
            get { return _currentStatus != null && _currentStatus.Length > 0 ? string.Concat(_currentStatus, " ", runnedConfig) : "Остановлено"; }
        }
        public string UsedConfig
        {
            get { return _selectedConfig != null ? string.Concat("Выбранный конфиг: ", _selectedConfig.Name) : "НЕ ВЫБРАН КОНФИГ"; }
        }
        #endregion

        #region Команды на интерфесы
        public RelayCommand ConnectVC { get; set; }
        public RelayCommand LogsVC { get; set; }
        public RelayCommand SettingsVC { get; set; }
        public RelayCommand DonateVC { get; set; }
        public RelayCommand TGProxyVC { get; set; }
        public RelayCommand PingVC { get; set; }

        public ConnectionsViewModel ConnectionsVM { get; set; }
        public LogsViewModel LogsVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }
        public DonateViewModel DonateVM { get; set; }
        public TGProxyViewModel TGProxyVM { get; set; }
        public PingViewModel PingVM { get; set; }
        #endregion

        #region Команды
        public RelayCommand SetStartUp { get; set; }
        #region Zapret
        public RelayCommand StartZapret { get; set; }
        public RelayCommand StopZapret { get; set; }
        public RelayCommand RestartZapret { get; set; }
        public RelayCommand SetAutoConnect { get; set; }
        public RelayCommand SetAutoRestartIfError { get; set; }
        #endregion

        #region Ping
        public RelayCommand StartPing { get; set; }
        public RelayCommand StartPingAllCfg { get; set; }
        #endregion

        #region TG
        public RelayCommand SetAutoStartTGProxy { get; set; }
        public RelayCommand StartTgProxy { get; set; }
        public RelayCommand StopTgProxy { get; set; }
        #endregion
        #endregion

        #region Обновление UI и переменные
        private object? _currentView;
        public object? CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged("CurrentView"); }
        }

        private ConfigInfo? _selectedConfig;
        public ConfigInfo SelectedConfig
        {
            get { return _selectedConfig; }
            set { _selectedConfig = value; OnPropertyChanged(nameof(UsedConfig)); OnPropertyChanged(nameof(SelectedConfig)); }
        }

        private string? _currentStatus;
        public string CurrentStatus
        {
            get { return _currentStatus; }
            set { _currentStatus = value; OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(CurrentStatus)); }
        }

        #region Переменные
        private AppConfig appConfig;
        private bool _autoStartUp;
        public bool AutoStartUp
        {
            get => _autoStartUp;
            set
            {
                _autoStartUp = value;
                OnPropertyChanged(nameof(AutoStartUp));
            }
        }

        private bool _autoConnect;
        public bool AutoConnect
        {
            get => _autoConnect;
            set
            {
                _autoConnect = value;
                OnPropertyChanged(nameof(AutoConnect));
            }
        }

        private bool _autoRestartIfError;
        public bool AutoRestartIfError
        {
            get => _autoRestartIfError;
            set
            {
                _autoRestartIfError = value;
                OnPropertyChanged(nameof(AutoRestartIfError));
            }
        }

        private bool _tgProxyAutoStart;
        public bool TgProxyAutoStart
        {
            get => _tgProxyAutoStart;
            set 
            {
                _tgProxyAutoStart = value;
                OnPropertyChanged(nameof(TgProxyAutoStart));
            }
        }

        private string _tgProxyIsStart = "Статус: Прокси не запущен";
        private bool _tgProxyIsStartBool = false;
        public string TgProxyIsStart
        {
            get => _tgProxyIsStart;
            set
            {
                _tgProxyIsStart = value;
                _tgProxyIsStartBool = !_tgProxyIsStartBool;
                OnPropertyChanged(nameof(_tgProxyIsStartBool));
                OnPropertyChanged(nameof(TgProxyIsStart));
            }
        }
        #endregion
        #endregion

        ZapretController controller = new("");
        ConfigEngine configEngine = new();
        public MainViewModel()
        {
            _proxyService = new TcpServerService();
            #region Config
            appConfig = configEngine.LoadConfig();
            _autoConnect = appConfig.AutoConnect;
            _autoStartUp = appConfig.StartUpWithSystem;
            _autoRestartIfError = appConfig.AutoRestartIfError;
            _tgProxyAutoStart = appConfig.AutoStartTGProxy;

            SetStartUp = new RelayCommand(o =>
            {
                configEngine.SetStartUpWithSystem(AutoStartUp, appConfig);
            });

            SetAutoRestartIfError = new RelayCommand(o =>
            {
                configEngine.SetAutoRestartIfError(AutoRestartIfError, appConfig);
            });

            SetAutoConnect = new RelayCommand(o =>
            {
                if (_selectedConfig != null && _selectedConfig.ConfigPath.Length > 0)
                {
                    configEngine.SetAutoConnect(_selectedConfig, AutoConnect, appConfig);
                }
                else
                {
                    MessageBox.Show("Чтобы установить это значение,\nподключитесь хоть к одному конфигу!\nПосле этого перенажмите данную кнопку.");
                }
            });

            SetAutoStartTGProxy = new RelayCommand(o =>
            {
                configEngine.SetAutoStartTGProxy(TgProxyAutoStart, appConfig);
            });

            if (AutoConnect)
            {
                _selectedConfig = appConfig.LastConfig;

                runnedConfig = _selectedConfig.Name;
                controller = new(_selectedConfig.ConfigPath);
                controller.OnLogReceived += (msg) => AddLogEntry(msg);
                controller.Start();
                CurrentStatus = "Запущено";
            }

            if (TgProxyAutoStart)
            {
                AddLogEntry("[СИСТЕМА] Автозапуск Telegram Proxy...");
                StartServer();
                AddLogEntry("[СИСТЕМА] Telegram Proxy запущен.");
            }
            #endregion

            ConnectionsVM = new ConnectionsViewModel();
            LogsVM = new LogsViewModel();
            SettingsVM = new SettingsViewModel();
            TGProxyVM = new TGProxyViewModel();
            DonateVM = new DonateViewModel();
            PingVM = new PingViewModel();

            CurrentView = ConnectionsVM;

            Configs = Helper.Configs;

            #region Zapret
            StartZapret = new RelayCommand(o =>
            {
                Helper.CheckDriver();
                if (controller.IsRunning)
                {
                    controller.Stop();
                }

                if (_selectedConfig != null && _selectedConfig.ConfigPath.Length > 0)
                {
                    if (appConfig.AutoConnect)
                    {
                        configEngine.SetAutoConnect(_selectedConfig, AutoConnect, appConfig);
                    }

                    runnedConfig = _selectedConfig.Name;
                    controller = new(_selectedConfig.ConfigPath);
                    controller.OnLogReceived += (msg) => AddLogEntry(msg);
                    controller.Start();
                    CurrentStatus = "Запущено";
                    Helper.ShowMessage($"Конфиг запущен!", _selectedConfig.Name, "Запуск движка");
                }
            });

            StopZapret = new RelayCommand(o =>
            {
                if (controller.IsRunning)
                {
                    controller.Stop();
                    CurrentStatus = "Остановлено";
                    Helper.ShowMessage("Конфиг остановлен!", Header: "Остановка движка");
                }
            });

            RestartZapret = new RelayCommand(o =>
            {
                if (controller.IsRunning)
                {
                    Helper.ShowMessage("Перезагрузка конфига!");
                    CurrentStatus = "Перезагрузка...";
                    controller.Stop();
                    Helper.CheckDriver();
                    controller.Start();
                    CurrentStatus = "Запущено";
                    Helper.ShowMessage("Конфиг перезапущен!");
                }
            });
            #endregion

            #region Ping
            StartPing = new RelayCommand(async(o) => await RunPingTest());
            StartPingAllCfg = new RelayCommand(async (o) => 
            {
                PingLogs.Clear();
                AddLogEntry("[СИСТЕМА] Запущена проверка пинга всех конфигов...");
                await RunPingTest(true);
                AddLogEntry("[СИСТЕМА] Проверка пинга завершена!");
            });
            #endregion

            #region TG
            StartTgProxy = new RelayCommand(o => { StartServer(); AddLogEntry("[СИСТЕМА] Telegram Proxy запущен."); });
            StopTgProxy = new RelayCommand(o => { StopServer(); AddLogEntry("[СИСТЕМА] Telegram Proxy остановлен."); });
            #endregion

            #region Панели
            ConnectVC = new RelayCommand(o =>
            {
                CurrentView = ConnectionsVM;
            });

            LogsVC = new RelayCommand(o =>
            {
                CurrentView = LogsVM;
            });

            SettingsVC = new RelayCommand(o =>
            {
                CurrentView = SettingsVM;
            });

            TGProxyVC = new RelayCommand(o =>
            {
                CurrentView = TGProxyVM;
            });

            DonateVC = new RelayCommand(o =>
            {
                CurrentView = DonateVM;
            });

            PingVC = new RelayCommand(o => 
            {
                CurrentView = PingVM;
            });
            #endregion
        }

        #region Методы
        public void AddLogEntry(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                string type = "Info";
                if (message.Contains("[ОШИБКА]") || message.Contains("cannot apply dupsid tls mod. payload is not valid tls."))
                {
                    type = "Error";
                }
                else if (message.Contains("[СИСТЕМА]") || message.Contains("[СТАРТ]"))
                {
                    type = "System";
                }

                Logs.Add(new LogMessage
                {
                    Text = message,
                    Time = DateTime.Now,
                    Type = type
                });

                if (AutoRestartIfError && type == "Error")
                {
                    CurrentStatus = "Перезагрузка...";
                    controller.Stop();
                    controller.Start();
                    CurrentStatus = "Запущено";
                }

                if (Logs.Count > 750) Logs.RemoveAt(0);
            });
        }

        #region Ping
        public async Task RunPingTest(bool TestAllConfigs = false)
        {
            Network network = new Network();

            if (!TestAllConfigs)
            {
                Application.Current.Dispatcher.Invoke(() => PingLogs.Clear());

                List<DomainInfo> results = await network.CheckDomainsAsync(InternalData.Domains);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PingLogs.Add(new DomainInfo { Name = string.Concat("Тестируем ", _selectedConfig.Name, " конфиг"), IsTest = true });
                    foreach (var domain in results)
                    {
                        PingLogs.Add(domain);
                    }
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => 
                { 
                    PingLogs.Clear();
                    PingLogs.Add(new DomainInfo { Name = "Запуск массового теста", IsTest = true });
                });

                foreach (ConfigInfo cfg in Configs)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PingLogs.Add(new DomainInfo { Name = string.Concat("Тестируем ", cfg.Name, " конфиг"), IsTest = true });
                    });

                    if (controller != null)
                    {
                        if (controller.IsRunning)
                        {
                            controller.Stop();
                        }
                        controller.Dispose();
                        Helper.CheckDriver();
                    }

                    controller = new ZapretController(cfg.ConfigPath);
                    controller.OnLogReceived += (msg) => AddLogEntry(msg);
                    controller.Start();

                    // Время для запуска winws
                    await Task.Delay(3000);

                    List<DomainInfo> results = await network.CheckDomainsAsync(InternalData.Domains, 1250);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var domain in results)
                        {
                            PingLogs.Add(domain);
                        }
                    });
                }

                if (controller != null)
                {
                    if (controller.IsRunning)
                    {
                        controller.Stop();
                    }
                    controller.Dispose();
                    Helper.CheckDriver();
                }

                controller = new ZapretController(_selectedConfig.ConfigPath);
                controller.OnLogReceived += (msg) => AddLogEntry(msg);
                controller.Start();

                PingLogs.Add(new DomainInfo { Name = "Тестирование завершено!", IsTest = true });
                Helper.ShowMessage("Тестирование завершено!", "Вернули конфиг: " + _selectedConfig.Name);
            }
        }
        #endregion

        #region Прокси для телеги
        private async void StartServer()
        {
            if (!_tgProxyIsStartBool)
            {
                TgProxyIsStart = "Статус: Прокси запущен";
                _cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    await _proxyService.StartListeningAsync(_cancellationTokenSource.Token);
                    Helper.ShowMessage("Прокси запущен!");
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message); }
                finally
                {
                    TgProxyIsStart = "Статус: Прокси не запущен";
                    Helper.ShowMessage("Прокси был выключен!");
                }
            }
        }

        private void StopServer()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                Helper.ShowMessage("Прокси был выключен!");
            }
        }
        #endregion
        #endregion
    }
}

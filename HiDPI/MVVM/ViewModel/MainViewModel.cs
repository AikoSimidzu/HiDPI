namespace HiDPI.MVVM.ViewModel
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using HiDPI.BackEnd;
    using HiDPI.MVVM.Core;

    class MainViewModel : ObservableObject
    {
        #region Бинды
        public ObservableCollection<LogMessage> Logs { get; } = new ObservableCollection<LogMessage>();
        public ObservableCollection<ConfigInfo> Configs { get; set; }

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

        public ConnectionsViewModel ConnectionsVM { get; set; }
        public LogsViewModel LogsVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }
        public DonateViewModel DonateVM { get; set; }
        #endregion

        #region Команды
        public RelayCommand StartZapret { get; set; }
        public RelayCommand StopZapret { get; set; }
        public RelayCommand RestartZapret { get; set; }
        public RelayCommand SetStartUp { get; set; }
        public RelayCommand SetAutoConnect { get; set; }
        public RelayCommand SetAutoRestartIfError { get; set; }
        #endregion

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

        private AppConfig appConfig;
        private bool _autoStartUp;
        public bool AutoStartUp
        {
            get { return _autoStartUp; }
            set
            {
                _autoStartUp = value;
                OnPropertyChanged(nameof(AutoStartUp));
            }
        }

        private bool _autoConnect;
        public bool AutoConnect
        {
            get { return _autoConnect; }
            set
            {
                _autoConnect = value;
                OnPropertyChanged(nameof(AutoConnect));
            }
        }

        private bool _autoRestartIfError;
        public bool AutoRestartIfError
        {
            get { return _autoRestartIfError; }
            set 
            {
                _autoRestartIfError = value;
                OnPropertyChanged(nameof(AutoRestartIfError));
            }
        }

        ZapretController controller = new("");
        ConfigEngine configEngine = new();
        public MainViewModel()
        {
            #region Config
            appConfig = configEngine.LoadConfig();
            _autoConnect = appConfig.AutoConnect;
            _autoStartUp = appConfig.StartUpWithSystem;
            _autoRestartIfError = appConfig.AutoRestartIfError;

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

            if (AutoConnect)
            {
                _selectedConfig = appConfig.LastConfig;

                runnedConfig = _selectedConfig.Name;
                controller = new(_selectedConfig.ConfigPath);
                controller.OnLogReceived += (msg) => AddLogEntry(msg);
                controller.Start();
                CurrentStatus = "Запущено";
            }
            #endregion

            ConnectionsVM = new ConnectionsViewModel();
            LogsVM = new LogsViewModel();
            SettingsVM = new SettingsViewModel();
            DonateVM = new DonateViewModel();
            
            CurrentView = ConnectionsVM;

            Configs = Helper.Configs;

            StartZapret = new RelayCommand(o =>
            {
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
                }
            });

            StopZapret = new RelayCommand(o => 
            {
                if(controller.IsRunning)
                {  
                    controller.Stop();
                    CurrentStatus = "Остановлено";
                }
            });

            RestartZapret = new RelayCommand(o => 
            {
                if (controller.IsRunning)
                {
                    CurrentStatus = "Перезагрузка...";
                    controller.Stop();
                    controller.Start();
                    CurrentStatus = "Запущено";
                }
            });

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

            DonateVC = new RelayCommand(o =>
            {
                CurrentView = DonateVM;
            });
        }

        public void AddLogEntry(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //cannot apply dupsid tls mod. payload is not valid tls.
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

                if (Logs.Count > 200) Logs.RemoveAt(0);
            });
        }
    }
}

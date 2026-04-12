namespace HiDPI.BackEnd
{
    using System.Net.NetworkInformation;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement;

    public enum BypassStatus
    {
        Success,              // Успешный успех
        MitM,                 // Заглушка провайдера
        Timeout,              // Дроп
        ConnectionReset,      // Соединение сброшено
        ProtocolError,        // Блокировка протокола
        DnsError,             // Блокировка на уровне DNS
        UnknownError
    }

    /// <summary>
    /// Инфо о домене
    /// </summary>
    /// <param name="Name">Короткое имя домена</param>
    /// <param name="Url">Ссылка</param>
    /// <param name="Keyword">Слово проверки</param>
    /// <param name="Ping">Отклик</param>
    /// <param name="ByTCP">Определяем протокол подключение</param>
    /// <param name="BypassState">enum статуса</param>
    /// <param name="IsSuccess">Bool значение успеха</param>
    /// <param name="IsTest">Просто маркер для ListBox</param>
    public class DomainInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Keyword {  get; set; } = string.Empty;
        public long Ping { get; set; } = 0;
        public bool ByTCP { get; set; } = true;
        public BypassStatus BypassState { get; set; }
        public bool IsSuccess => BypassState == BypassStatus.Success;
        public bool IsTest { get; set; } = false;

        public string DisplayStatus
        {
            get
            {
                if (IsSuccess)
                {
                    return $"{Ping} мс";
                }

                return BypassState switch
                {
                    BypassStatus.Timeout => "Дроп",
                    BypassStatus.ConnectionReset => "Сброс",
                    BypassStatus.MitM => "FAKE PAGE",
                    BypassStatus.ProtocolError => "Блок протокола",
                    BypassStatus.DnsError => "Ошибка DNS",
                    _ => "Неизвестная ошибка"
                };
            }
        }
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

namespace TGProxy
{
#if ANDROID
    using Android.Content;
#endif

    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();

                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlertAsync("Внимание", "Без разрешения на уведомления прокси может быть убит системой в фоне!", "ОК");
                    }
                }
            }
#endif
        }

        private void startProxy_Clicked(object sender, EventArgs e)
        {
#if ANDROID
            var intent = new Intent(Android.App.Application.Context, typeof(Platforms.Android.ProxyService));
            Android.App.Application.Context.StartForegroundService(intent);
#endif
        }

        private async void connectToProxy_Clicked(object sender, EventArgs e)
        {
            string server = "127.0.0.1";
            int port = 1080;
            string secret = "00000000000000000000000000000000";

            // Формируем нативный Deep Link для Telegram
            string tgUrl = $"tg://proxy?server={server}&port={port}&secret={secret}";

            // Запасной вариант через web-ссылку (на случай багов ОС)
            string webUrl = $"https://t.me/proxy?server={server}&port={port}&secret={secret}";

            try
            {
                // Проверяем, знает ли Android, как открывать ссылки tg:// (установлен ли клиент)
                bool supportsTg = await Launcher.Default.CanOpenAsync(tgUrl);

                if (supportsTg)
                {
                    // Открываем нативно
                    await Launcher.Default.OpenAsync(tgUrl);
                }
                else
                {
                    // Если tg:// не распознан, открываем t.me ссылку (она откроет браузер, а браузер уже перекинет в ТГ)
                    await Launcher.Default.OpenAsync(webUrl);
                }
            }
            catch (Exception ex)
            {
                // На случай, если на устройстве вообще нет браузера или произошла ошибка
                await DisplayAlert("Ошибка", $"Не удалось открыть Telegram: {ex.Message}", "ОК");
            }
        }
    }
}

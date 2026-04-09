namespace TGProxy.Platforms.Android
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using AndroidX.Core.App;
    using global::Android.App;
    using global::Android.Content;
    using global::Android.Graphics;
    using global::Android.OS;
    using TGProxy.BackEnd.TG;

    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
    public class ProxyService : Service
    {
        public const string ActionStopService = "STOP_PROXY_SERVICE";

        private TcpListener _listener;
        private bool _isRunning;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent?.Action == ActionStopService)
            {
                StopProxy();
                return StartCommandResult.NotSticky;
            }

            if (_isRunning) return StartCommandResult.Sticky;

            var notification = CreateNotification();
            StartForeground(1001, notification);
            _isRunning = true;

            Task.Run(StartServerAsync);

            return StartCommandResult.Sticky;
        }

        private async Task StartServerAsync()
        {
            _listener = new TcpListener(IPAddress.Loopback, 1080);
            _listener.Start();
            var proxy = new TGProxy();

            try
            {
                while (_isRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    client.NoDelay = true;
                    client.ReceiveBufferSize = 65536;
                    client.SendBufferSize = 65536;
                    _ = proxy.HandleClientAsync(client);
                }
            }
            catch (SocketException)
            {

            }
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            var notification = CreateNotification();
            StartForeground(1001, notification);

            base.OnTaskRemoved(rootIntent);
        }

        private void StopProxy()
        {
            _isRunning = false;
            _listener?.Stop();

            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
        }

        public override void OnDestroy()
        {
            StopProxy();
            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent) => null;

        private Notification CreateNotification()
        {
            string channelId = "proxy_service_channel";
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel channel = new(channelId, "MTProto Proxy", NotificationImportance.Low);
                channel.SetSound(null, null);
                channel.EnableVibration(false);

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager.CreateNotificationChannel(channel);
            }

            var stopIntent = new Intent(this, typeof(ProxyService));
            stopIntent.SetAction(ActionStopService);

            var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;

            var stopPendingIntent = PendingIntent.GetService(this, 0, stopIntent, pendingIntentFlags);

            return new NotificationCompat.Builder(this, channelId)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetContentTitle("Локальный MTProto Proxy")
                .SetContentText("Работает на 127.0.0.1:1080")
                .SetSmallIcon(Resource.Drawable.hidpiicon)
                .SetColor(Color.ParseColor("#2D2D2D"))
                .SetOngoing(true)
                .AddAction(0, "Остановить", stopPendingIntent)
                .Build();
        }
    }
}
namespace HiDPI
{
    using System.Runtime.InteropServices;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = new Mutex(true, "{a386cbdd71d343d49bf4c5a5d248a4c1}");

        public const int WM_SHOWME = 0x1122;
        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32.dll")]
        public static extern int RegisterWindowMessage(string messageName);

        private static int _showMeMessage = RegisterWindowMessage("WM_SHOW_MY_WPF_APP");

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                PostMessage((IntPtr)0xffff, _showMeMessage, IntPtr.Zero, IntPtr.Zero);
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }

}

namespace HiDPI
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using HiDPI.BackEnd;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Helper.InitializeConfigMonitoring();
        }

        #region Кнопки
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ToTray_Click(object sender, RoutedEventArgs e)
        {
            RefreshVisibility(false);
            this.Hide();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        #region Tray
        #region База
        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
            RefreshVisibility(true);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            RefreshVisibility(true);
            Application.Current.Shutdown();
        }

        private void HiDPIIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
            RefreshVisibility(true);
        }

        public void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }

        private void RefreshVisibility(bool isWindowVisible)
        {
            if (isWindowVisible)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                HiDPIIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.Hide();
                HiDPIIcon.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Открытие через EXE
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == App.WM_SHOWME)
            {
                ShowAndActivate();
            }
            return IntPtr.Zero;
        }

        private void ShowAndActivate()
        {
            if (!this.IsVisible)
            {
                this.Show();
            }

            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Activate();
            this.Focus();
        }
        #endregion

        #endregion
    }
}
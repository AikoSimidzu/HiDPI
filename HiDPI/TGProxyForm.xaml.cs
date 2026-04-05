namespace HiDPI
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Логика взаимодействия для TGProxyForm.xaml
    /// </summary>
    public partial class TGProxyForm : UserControl
    {
        public TGProxyForm()
        {
            InitializeComponent();
        }

        private void AddToTG_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://t.me/proxy?server=127.0.0.1&port=1080&secret=00000000000000000000000000000000", UseShellExecute = true });
        }
    }
}

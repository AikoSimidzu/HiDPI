namespace HiDPI
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Логика взаимодействия для DonateForm.xaml
    /// </summary>
    public partial class DonateForm : UserControl
    {
        public DonateForm()
        {
            InitializeComponent();
        }

        private void donate_Click(object sender, RoutedEventArgs e)
        {
            image.Visibility = Visibility.Collapsed;
            imageThx.Visibility = Visibility.Visible;
            Process.Start(new ProcessStartInfo { FileName = "https://www.donationalerts.com/r/yatosimidzu", UseShellExecute = true });
        }
    }
}

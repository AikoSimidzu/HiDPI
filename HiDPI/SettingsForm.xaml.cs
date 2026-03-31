namespace HiDPI
{
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using HiDPI.BackEnd;

    /// <summary>
    /// Логика взаимодействия для SettingsForm.xaml
    /// </summary>
    public partial class SettingsForm : UserControl
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private static string _actualVersion = string.Empty;
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            VersionNow.Content += BackEnd.InternalData.CurrentVersion;
            _actualVersion = await Helper.GetActualVersion();
            ActualVersion.Content += _actualVersion;
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe"), _actualVersion);
            Environment.Exit(10);
        }
    }
}

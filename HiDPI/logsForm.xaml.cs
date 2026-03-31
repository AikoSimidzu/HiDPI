namespace HiDPI
{
    using System.Collections.Specialized;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using HiDPI.BackEnd;

    public partial class logsForm : UserControl
    {
        public logsForm()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)LogList.Items).CollectionChanged += LogList_CollectionChanged;
        }

        private void LogList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (LogList.Items.Count > 0)
                {
                    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (LogList.ItemsSource is IEnumerable<LogMessage> sourceCollection)
            {
                List<string> logs = sourceCollection.Select(item => item.Text).ToList();
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LOGS.txt"), logs);
            }
        }
    }
}

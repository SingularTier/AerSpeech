using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using AerSpeech;

namespace AerWPF
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : UserControl 
    {

        public string FileText { get; set; }
        private ObservableCollection<LogEntry> _items;
        
        public Console()
        {
            InitializeComponent();

            AerDebug.OnLogSpeech += this.LogSpeech;
            AerDebug.OnLogSay += this.LogSay;
            AerDebug.OnLog += this.Log;
            _items = new ObservableCollection<LogEntry>();
            LogListBox.ItemsSource = _items;
            
        }

        public void Log(object sender, DebugLogEventArgs args)
        {
            LogEntry newEntry = new LogEntry() { Text = args.Text };
            newEntry.Foreground = "Yellow";
            newEntry.Justification = "Left";
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _items.Add(newEntry);
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            });
        }

        public void LogSpeech(object sender, DebugLogEventArgs args)
        {
            LogEntry newEntry = new LogEntry() { Text = args.Text };
            if (args.Accepted)
                newEntry.Foreground = "LightBlue";
            else
                newEntry.Foreground = "Khaki";

            newEntry.Justification = "Left";
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _items.Add(newEntry);
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            });
        }

        public void LogSay(object sender, DebugLogEventArgs args)
        {
            LogEntry newEntry = new LogEntry() { Text = args.Text };
            newEntry.Foreground = "Red";
            newEntry.Justification = "Right";
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _items.Add(newEntry);
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            });
        }
    }

    public class LogEntry
    {
        public string Text { get; set; }
        public string Justification { get; set; }
        public string Foreground { get; set; }
    }
}

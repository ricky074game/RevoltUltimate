using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Windows
{
    public partial class ConsoleWindow : Window
    {
        public ConsoleWindow()
        {
            InitializeComponent();

            // Attach the global trace listener to the TextBox
            App.GlobalTraceListener.AttachTextBox(OutputTextBox);

            // Load previous trace history into the TextBox
            App.GlobalTraceListener.LoadPreviousHistory();

            Closed += ConsoleWindow_Closed;
        }

        private void ConsoleWindow_Closed(object sender, EventArgs e)
        {
            // Detach the TextBox when the window is closed
            App.GlobalTraceListener.DetachTextBox();
        }
    }

    public class TextBoxTraceListener : TraceListener
    {
        private TextBox _outputTextBox;
        private static readonly List<string> TraceHistory = new();

        public TextBoxTraceListener(TextBox outputTextBox = null)
        {
            _outputTextBox = outputTextBox;
            Name = "TextBoxTraceListener";
        }

        public override void Write(string message)
        {
            AppendMessage(message);
        }

        public override void WriteLine(string message)
        {
            AppendMessage(message + Environment.NewLine);
        }

        private void AppendMessage(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";

            // Store the message in the history
            TraceHistory.Add(timestampedMessage);

            // Append the message to the TextBox if it's attached
            if (_outputTextBox != null)
            {
                _outputTextBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _outputTextBox.AppendText(timestampedMessage);
                    _outputTextBox.ScrollToEnd();
                }));
            }
        }

        public void LoadPreviousHistory()
        {
            if (_outputTextBox != null)
            {
                _outputTextBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var message in TraceHistory)
                    {
                        _outputTextBox.AppendText(message);
                    }
                    _outputTextBox.ScrollToEnd();
                }));
            }
        }

        public void AttachTextBox(TextBox textBox)
        {
            _outputTextBox = textBox;
        }

        public void DetachTextBox()
        {
            _outputTextBox = null;
        }
    }
}
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Setup
{
    public class InputFieldDefinition
    {
        public string Label { get; set; }
        public string DefaultValue { get; set; }
        public Func<string, bool> Validator { get; set; }
        public string ValidationErrorMessage { get; set; }
    }
    public partial class InputDialog : Window
    {
        private readonly List<InputFieldDefinition> fieldDefinitions;
        private readonly Dictionary<string, TextBox> textBoxes = new();
        public string HelpUrl { get; set; }

        public Dictionary<string, string> Responses { get; private set; } = new();

        public InputDialog(string question, List<InputFieldDefinition> fields, String helpUrl)
        {
            InitializeComponent();
            QuestionLabel.Text = question;
            fieldDefinitions = fields;
            HelpUrl = helpUrl;

            GenerateFields();
        }

        private void GenerateFields()
        {
            foreach (var field in fieldDefinitions)
            {
                var stackPanel = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };

                var label = new TextBlock
                {
                    Text = field.Label,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = System.Windows.Media.Brushes.White
                };
                stackPanel.Children.Add(label);

                var textBox = new TextBox
                {
                    Text = field.DefaultValue ?? string.Empty,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(textBox);

                FieldsContainer.Children.Add(stackPanel);
                textBoxes[field.Label] = textBox;
            }
        }
        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void QuestionMark_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(HelpUrl))
            {
                Process.Start(new ProcessStartInfo(HelpUrl) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("No help URL is set.", "Help", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Responses.Clear();

            foreach (var field in fieldDefinitions)
            {
                var textBox = textBoxes[field.Label];
                var value = textBox.Text;

                if (field.Validator != null && !field.Validator(value))
                {
                    MessageBox.Show(field.ValidationErrorMessage, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Responses[field.Label] = value;
            }

            DialogResult = true;
        }
    }
}
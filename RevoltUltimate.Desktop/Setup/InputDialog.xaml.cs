using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Setup
{
    public class InputFieldDefinition
    {
        public string Label { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public string? Placeholder { get; set; }
        public Func<string, bool>? Validator { get; set; }
        public string? ValidationErrorMessage { get; set; }
        public bool IsPassword { get; set; } = false;
    }

    public partial class InputDialog : Window
    {
        private readonly List<InputFieldDefinition> _fieldDefinitions;
        private readonly Dictionary<string, TextBox> _textBoxes = new();
        private readonly Dictionary<string, PasswordBox> _passwordBoxes = new();

        public string? HelpUrl { get; set; }
        public Dictionary<string, string> Responses { get; private set; } = new();

        public InputDialog(string question, List<InputFieldDefinition> fields, string? helpUrl = null)
        {
            InitializeComponent();
            QuestionLabel.Text = question;
            _fieldDefinitions = fields;
            HelpUrl = helpUrl;

            // Hide help button if no URL
            if (string.IsNullOrWhiteSpace(helpUrl))
            {
                HelpButton.Visibility = Visibility.Collapsed;
            }

            GenerateFields();
        }

        private void GenerateFields()
        {
            foreach (var field in _fieldDefinitions)
            {
                var fieldContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

                // Label
                var label = new TextBlock
                {
                    Text = field.Label,
                    Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245)),
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                fieldContainer.Children.Add(label);

                if (field.IsPassword)
                {
                    // Password Box
                    var passwordBox = new PasswordBox
                    {
                        Password = field.DefaultValue ?? string.Empty,
                        Style = CreatePasswordBoxStyle()
                    };
                    fieldContainer.Children.Add(passwordBox);
                    _passwordBoxes[field.Label] = passwordBox;
                }
                else
                {
                    // Regular TextBox with placeholder support
                    var textBoxContainer = new Grid();

                    var textBox = new TextBox
                    {
                        Text = field.DefaultValue ?? string.Empty,
                        Style = (Style)FindResource("ModernTextBoxStyle"),
                        Tag = field.Placeholder
                    };

                    // Placeholder TextBlock
                    var placeholder = new TextBlock
                    {
                        Text = field.Placeholder ?? string.Empty,
                        Foreground = new SolidColorBrush(Color.FromRgb(113, 113, 122)),
                        FontSize = 14,
                        IsHitTestVisible = false,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(14, 0, 0, 0),
                        Visibility = string.IsNullOrEmpty(field.DefaultValue) ? Visibility.Visible : Visibility.Collapsed
                    };

                    textBox.TextChanged += (s, e) =>
                    {
                        placeholder.Visibility = string.IsNullOrEmpty(textBox.Text)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    };

                    textBox.GotFocus += (s, e) =>
                    {
                        if (string.IsNullOrEmpty(textBox.Text))
                            placeholder.Visibility = Visibility.Collapsed;
                    };

                    textBox.LostFocus += (s, e) =>
                    {
                        if (string.IsNullOrEmpty(textBox.Text))
                            placeholder.Visibility = Visibility.Visible;
                    };

                    textBoxContainer.Children.Add(textBox);
                    textBoxContainer.Children.Add(placeholder);

                    fieldContainer.Children.Add(textBoxContainer);
                    _textBoxes[field.Label] = textBox;
                }

                FieldsContainer.Children.Add(fieldContainer);
            }
        }

        private Style CreatePasswordBoxStyle()
        {
            var style = new Style(typeof(PasswordBox));

            var template = new ControlTemplate(typeof(PasswordBox));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "border");
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(9, 9, 11)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(63, 63, 70)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            var scrollViewerFactory = new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost");
            scrollViewerFactory.SetValue(ScrollViewer.MarginProperty, new Thickness(12, 10, 12, 10));
            borderFactory.AppendChild(scrollViewerFactory);

            template.VisualTree = borderFactory;

            // Triggers
            var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(82, 82, 91)), "border"));
            template.Triggers.Add(mouseOverTrigger);

            var focusTrigger = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
            focusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(0, 120, 212)), "border"));
            focusTrigger.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "border"));
            template.Triggers.Add(focusTrigger);

            style.Setters.Add(new Setter(PasswordBox.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(PasswordBox.FontSizeProperty, 14.0));
            style.Setters.Add(new Setter(PasswordBox.TemplateProperty, template));

            return style;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void QuestionMark_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(HelpUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(HelpUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open help URL: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Responses.Clear();

            foreach (var field in _fieldDefinitions)
            {
                string value;

                if (field.IsPassword && _passwordBoxes.TryGetValue(field.Label, out var passwordBox))
                {
                    value = passwordBox.Password;
                }
                else if (_textBoxes.TryGetValue(field.Label, out var textBox))
                {
                    value = textBox.Text;
                }
                else
                {
                    continue;
                }

                if (field.Validator != null && !field.Validator(value))
                {
                    // Show validation error with a nicer message
                    ShowValidationError(field.Label, field.ValidationErrorMessage ?? "Invalid input");
                    return;
                }

                Responses[field.Label] = value;
            }

            DialogResult = true;
            Close();
        }

        private void ShowValidationError(string fieldLabel, string message)
        {
            // Find the field and highlight it
            if (_textBoxes.TryGetValue(fieldLabel, out var textBox))
            {
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                textBox.Focus();
            }
            else if (_passwordBoxes.TryGetValue(fieldLabel, out var passwordBox))
            {
                passwordBox.Focus();
            }

            MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
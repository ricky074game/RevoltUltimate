using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Setup.Login;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RevoltUltimate.Desktop.Setup.Steps
{
    public partial class LinkAccountsStep : UserControl
    {
        private Border activeLogoBox;
        private Popup dropdownMenu;
        private bool isDropdownJustOpened;
        private readonly Dictionary<string, List<string>> platformOptions = new()
        {
            { "Steam", new List<string> { "Login with Steam on Web", "Login with Steam API" } },
            { "GOG", new List<string> { "Login with GOG" } },
        };

        public ApplicationSettings Settings { get; set; }

        public static readonly RoutedEvent GoToNextStepEvent = EventManager.RegisterRoutedEvent(
            "GoToNextStep", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(LinkAccountsStep));
        public event RoutedEventHandler GoToNextStep
        {
            add { AddHandler(GoToNextStepEvent, value); }
            remove { RemoveHandler(GoToNextStepEvent, value); }
        }

        public LinkAccountsStep()
        {
            InitializeComponent();
            Settings = new ApplicationSettings();

            dropdownMenu = new Popup
            {
                Placement = PlacementMode.MousePoint,
                StaysOpen = true,
                Child = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(5),
                    Child = new StackPanel()
                }
            };
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.MouseDown += MainWindow_MouseDown;
            }
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dropdownMenu.IsOpen)
            {
                if (isDropdownJustOpened)
                {
                    isDropdownJustOpened = false;
                    return;
                }

                var hitTestResult = VisualTreeHelper.HitTest(dropdownMenu.Child, e.GetPosition(dropdownMenu.Child));
                if (hitTestResult == null)
                {
                    dropdownMenu.IsOpen = false;
                }
            }
        }

        private void LogoBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border logoBox)
            {
                activeLogoBox = logoBox;
                string platformName = logoBox.Tag as string;

                if (platformName != null && platformOptions.ContainsKey(platformName))
                {
                    if (dropdownMenu.IsOpen)
                    {
                        dropdownMenu.IsOpen = false;
                    }

                    var menu = dropdownMenu.Child as Border;
                    var stackPanel = menu?.Child as StackPanel;
                    stackPanel?.Children.Clear();

                    foreach (var option in platformOptions[platformName])
                    {
                        var checkBox = new CheckBox
                        {
                            Content = option,
                            Background = Brushes.Transparent,
                            Foreground = Brushes.White,
                            Margin = new Thickness(0, 5, 0, 5),
                            BorderBrush = Brushes.Gray,
                            BorderThickness = new Thickness(1),
                        };
                        checkBox.Checked += (s, args) => Option_Checked(s, args, platformName);
                        stackPanel?.Children.Add(checkBox);
                    }

                    isDropdownJustOpened = true;
                    dropdownMenu.IsOpen = true;
                }
            }
        }

        private async void Option_Checked(object sender, RoutedEventArgs e, string platformName)
        {
            if (sender is not CheckBox checkBox) return;

            var optionContent = checkBox.Content.ToString();
            dropdownMenu.IsOpen = false;

            switch (optionContent)
            {
                case "Login with Steam API":
                    var fields = new List<InputFieldDefinition>
                    {
                        new InputFieldDefinition
                        {
                            Label = "Steam API Key",
                            DefaultValue = Settings.SteamApiKey,
                            Validator = value => !string.IsNullOrWhiteSpace(value),
                            ValidationErrorMessage = "Steam API Key is required."
                        },
                        new InputFieldDefinition
                        {
                            Label = "Steam ID",
                            DefaultValue = Settings.SteamId,
                            Validator = value => !string.IsNullOrWhiteSpace(value),
                            ValidationErrorMessage = "Steam ID is required."
                        }
                    };

                    var inputDialog = new InputDialog("Please enter your Steam API Key and Steam ID:", fields, "https://github.com")
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (inputDialog.ShowDialog() == true)
                    {
                        Settings.SteamApiKey = inputDialog.Responses["Steam API Key"];
                        Settings.SteamId = inputDialog.Responses["Steam ID"];

                        if (!string.IsNullOrWhiteSpace(Settings.SteamApiKey) && !string.IsNullOrWhiteSpace(Settings.SteamId))
                        {
                            checkBox.IsChecked = true;
                        }
                        else
                        {
                            MessageBox.Show("Both Steam API Key and Steam ID are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    break;
                case "Login with Steam on Web":
                    var steamLoginWindow = new SteamWebLoginWindow()
                    {
                        Owner = Application.Current.MainWindow
                    };
                    steamLoginWindow.Show();
                    break;

                case "Login with GOG":
                    var gogLoginWindow = new GOGWebLoginWindow { Owner = Application.Current.MainWindow };
                    if (gogLoginWindow.ShowDialog() == true && !string.IsNullOrEmpty(gogLoginWindow.AuthCode))
                    {
                        try
                        {
                            var tokenResponse = await GOG.Instance.ExchangeAuthCodeForTokensAsync(gogLoginWindow.AuthCode);
                            GOG.Instance.SetSession(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.UserId);
                            var userData = await GOG.Instance.GetUserDataAsync();

                            if (userData != null && !string.IsNullOrEmpty(userData.Username))
                            {
                                AccountManager.SaveGOGTokens(userData.Username, tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.UserId);
                                MessageBox.Show($"GOG account for {userData.Username} linked successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                UpdateCheckmark(platformName);
                            }
                            else
                            {
                                throw new Exception("Failed to retrieve GOG user data.");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to link GOG account: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            checkBox.IsChecked = false;
                        }
                    }
                    else
                    {
                        checkBox.IsChecked = false;
                    }
                    return;
            }

            UpdateCheckmark(platformName);
        }

        private void UpdateCheckmark(string platformName)
        {
            Path? checkmark = null;
            switch (platformName)
            {
                case "Steam":
                    checkmark = FindName("Checksteam") as Path;
                    break;
                case "GOG":
                    checkmark = FindName("Checkgog") as Path;
                    break;
            }
            if (checkmark != null)
            {
                checkmark.Visibility = Visibility.Visible;
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(GoToNextStepEvent));
        }
    }
}
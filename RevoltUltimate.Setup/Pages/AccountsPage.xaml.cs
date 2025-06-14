using RevoltUltimate.API.Objects; // For User
// Remove: using RevoltUltimate.API.Searcher; // No longer directly using MainSteam
using RevoltUltimate.Setup.Windows; // For the local linksteamwindow
using System.IO; // For Path
using System.Text.Json; // For JsonSerializer
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Setup.Pages
{
    public partial class AccountsPage : Page
    {
        private SetupWindow _setupWindowInstance;
        private User _currentUser;
        private readonly string _settingsFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "settings.json");


        public AccountsPage(SetupWindow setupWindow, User user)
        {
            InitializeComponent();
            _setupWindowInstance = setupWindow;
            _currentUser = user;
            UpdateLinkSteamButtonText();
        }

        private void LinkSteamButton_Click(object sender, RoutedEventArgs e)
        {
            // linksteamwindow is now from RevoltUltimate.Setup.Windows
            linksteamwindow linksteamwindow = new linksteamwindow
            {
                Owner = _setupWindowInstance
            };

            linksteamwindow.ShowDialog(); // ShowDialog is blocking

            // After the dialog is closed, check if settings were saved
            if (linksteamwindow.SettingsSavedSuccessfully)
            {
                MessageBox.Show("Steam account details saved. They will be used when the main application starts.", "Steam Link", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            // No direct MainSteam interaction here.
            UpdateLinkSteamButtonText(); // Refresh button state based on saved file
        }

        private void UpdateLinkSteamButtonText()
        {
            // Check the settings file directly to see if credentials are saved
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    ApplicationSettings? settings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                    if (settings != null && !string.IsNullOrWhiteSpace(settings.SteamApiKey) && !string.IsNullOrWhiteSpace(settings.SteamId))
                    {
                        LinkSteamButton.Content = "Steam Details Saved";
                        // LinkSteamButton.IsEnabled = false; // Or allow re-editing
                    }
                    else
                    {
                        LinkSteamButton.Content = "Link Steam";
                    }
                }
                catch
                {
                    LinkSteamButton.Content = "Link Steam (Error reading settings)";
                }
            }
            else
            {
                LinkSteamButton.Content = "Link Steam";
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            // linksteamwindow has already saved settings to settings.json.
            // The main application will load these settings on its startup.
            _setupWindowInstance?.CompleteSetupAndLaunchMainApplication(_currentUser);
        }
    }
}
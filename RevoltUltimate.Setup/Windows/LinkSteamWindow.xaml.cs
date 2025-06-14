using System.IO;
using System.Text.Json;
using System.Windows;

namespace RevoltUltimate.Setup.Windows
{
    public partial class linksteamwindow : Window
    {
        private readonly string _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "settings.json");

        public string EnteredApiKey { get; private set; }
        public string EnteredSteamId { get; private set; }
        public bool SettingsSavedSuccessfully { get; private set; } = false;


        public linksteamwindow()
        {
            InitializeComponent();
            LoadCurrentSettingsFromFile();
        }

        private void LoadCurrentSettingsFromFile()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    // Use the local ApplicationSettings for deserialization
                    ApplicationSettings? existingSettings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                    if (existingSettings != null)
                    {
                        ApiKeyTextBox.Text = existingSettings.SteamApiKey;
                        SteamIdTextBox.Text = existingSettings.SteamId;
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle error if settings file is corrupt or unreadable
                    System.Diagnostics.Debug.WriteLine($"Error loading existing settings in setup: {ex.Message}");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            EnteredApiKey = ApiKeyTextBox.Text;
            EnteredSteamId = SteamIdTextBox.Text;

            ApplicationSettings settings;

            // Try to load existing settings to preserve other values like Version, CustomAnimationDllPath
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string existingJson = File.ReadAllText(_settingsFilePath);
                    settings = JsonSerializer.Deserialize<ApplicationSettings>(existingJson) ?? new ApplicationSettings();
                }
                catch
                {
                    settings = new ApplicationSettings(); // Fallback if existing is corrupt
                }
            }
            else
            {
                settings = new ApplicationSettings();
            }

            settings.SteamApiKey = EnteredApiKey;
            settings.SteamId = EnteredSteamId;
            // Version and other settings will be preserved if they existed, or use defaults from ApplicationSettings constructor

            try
            {
                string? directoryPath = Path.GetDirectoryName(_settingsFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                string jsonToSave = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, jsonToSave);

                SettingsSavedSuccessfully = true; // Indicate success
                DialogResult = true; // Close dialog
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SettingsSavedSuccessfully = false;
                DialogResult = false; // Keep dialog open or indicate failure
            }
        }
    }
}
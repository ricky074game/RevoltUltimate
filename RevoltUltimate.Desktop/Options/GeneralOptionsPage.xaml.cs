using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace RevoltUltimate.Desktop.Options
{
    public partial class GeneralOptionsPage : UserControl
    {
        private const string AppName = "RevoltUltimate";
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private bool _isInitializing = true;
        private bool _initialHardwareAcceleration;

        public GeneralOptionsPage()
        {
            InitializeComponent();
            Loaded += GeneralOptionsPage_Loaded;
        }

        private void GeneralOptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            _isInitializing = false;
        }

        private void LoadSettings()
        {
            if (App.Settings == null) return;

            StartWithWindowsCheckBox.IsChecked = App.Settings.StartWithWindows;
            StartMinimizedCheckBox.IsChecked = App.Settings.StartMinimized;
            MinimizeToTrayCheckBox.IsChecked = App.Settings.MinimizeToTray;
            HardwareAccelerationCheckBox.IsChecked = App.Settings.HardwareAccelerationEnabled;
            _initialHardwareAcceleration = App.Settings.HardwareAccelerationEnabled;
        }

        private void SaveSettings()
        {
            if (App.Settings == null || _isInitializing) return;

            try
            {
                string settingsFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevoltUltimate",
                    "settings.json");

                string? directoryPath = Path.GetDirectoryName(settingsFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(App.Settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);
                Trace.WriteLine("General settings saved successfully.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error saving general settings: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartWithWindowsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || App.Settings == null) return;

            bool startWithWindows = StartWithWindowsCheckBox.IsChecked == true;
            App.Settings.StartWithWindows = startWithWindows;

            try
            {
                SetStartWithWindows(startWithWindows);
                SaveSettings();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error setting startup registry: {ex.Message}");
                MessageBox.Show($"Failed to modify startup settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _isInitializing = true;
                StartWithWindowsCheckBox.IsChecked = !startWithWindows;
                App.Settings.StartWithWindows = !startWithWindows;
                _isInitializing = false;
            }
        }

        private void SetStartWithWindows(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null)
            {
                throw new InvalidOperationException("Could not open startup registry key.");
            }

            if (enable)
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppName}.exe");
                string arguments = App.Settings?.StartMinimized == true ? " --minimized" : "";
                key.SetValue(AppName, $"\"{exePath}\"{arguments}");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }

        private void StartMinimizedCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || App.Settings == null) return;

            App.Settings.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
            SaveSettings();
            if (App.Settings.StartWithWindows)
            {
                try
                {
                    SetStartWithWindows(true);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error updating startup registry: {ex.Message}");
                }
            }
        }

        private void MinimizeToTrayCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || App.Settings == null) return;

            App.Settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
            SaveSettings();
        }

        private void HardwareAccelerationCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || App.Settings == null) return;

            App.Settings.HardwareAccelerationEnabled = HardwareAccelerationCheckBox.IsChecked == true;
            SaveSettings();
            bool settingChanged = App.Settings.HardwareAccelerationEnabled != _initialHardwareAcceleration;
            HardwareAccelerationWarning.Visibility = settingChanged ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
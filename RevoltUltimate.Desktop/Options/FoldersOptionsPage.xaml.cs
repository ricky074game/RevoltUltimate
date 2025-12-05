using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace RevoltUltimate.Desktop.Options
{
    public partial class FoldersOptionsPage : UserControl
    {
        private ObservableCollection<string> _folders = new();

        private static readonly List<string> DefaultFolders = new()
        {
            @"%PUBLIC%\Documents\Steam\CODEX",
            @"%APPDATA%\Goldberg SteamEmu Saves",
            @"%APPDATA%\EMPRESS",
            @"%PUBLIC%\EMPRESS",
            @"%APPDATA%\Steam\Codex",
            @"%PROGRAMDATA%\Steam",
            @"%LOCALAPPDATA%\SKIDROW",
            @"%USERPROFILE%\Documents\SkidRow",
            @"%APPDATA%\SmartSteamEmu",
            @"%APPDATA%\CreamAPI",
            @"%APPDATA%\GSE Saves"
        };

        public FoldersOptionsPage()
        {
            InitializeComponent();
            Loaded += FoldersOptionsPage_Loaded;
        }

        private void FoldersOptionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFolders();
        }

        private void LoadFolders()
        {
            _folders.Clear();

            if (App.Settings?.WatchedFolders != null)
            {
                foreach (var folder in App.Settings.WatchedFolders)
                {
                    _folders.Add(folder);
                }
            }

            FoldersListBox.ItemsSource = _folders;
            UpdateFolderCounts();
        }

        private void UpdateFolderCounts()
        {
            int total = _folders.Count;
            int existing = _folders.Count(f => Directory.Exists(Environment.ExpandEnvironmentVariables(f)));

            FolderCountText.Text = $"{total} folder{(total != 1 ? "s" : "")}";
            ExistingCountText.Text = $"({existing} exist)";
        }

        private void SaveFolders()
        {
            if (App.Settings == null) return;

            App.Settings.WatchedFolders = _folders.ToList();

            try
            {
                string settingsFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevoltUltimate", "settings.json");

                string? directoryPath = Path.GetDirectoryName(settingsFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(App.Settings,
                    Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);

                Trace.WriteLine("Watched folders saved.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error saving folders: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string selectedPath = dialog.SelectedPath;

                if (_folders.Any(f =>
                    string.Equals(Environment.ExpandEnvironmentVariables(f), selectedPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f, selectedPath, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("This folder is already in the list.", "Duplicate Folder",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _folders.Add(selectedPath);
                SaveFolders();
                UpdateFolderCounts();
            }
        }

        private void RemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string folder)
            {
                _folders.Remove(folder);
                SaveFolders();
                UpdateFolderCounts();
            }
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to reset to the default folder list?\n\nThis will remove any custom folders you've added.",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _folders.Clear();

                foreach (var folder in DefaultFolders)
                {
                    _folders.Add(Environment.ExpandEnvironmentVariables(folder));
                }

                SaveFolders();
                UpdateFolderCounts();
                Trace.WriteLine("Watched folders reset to defaults.");
            }
        }
    }
    public class FolderExistsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(path);
                return Directory.Exists(expandedPath);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
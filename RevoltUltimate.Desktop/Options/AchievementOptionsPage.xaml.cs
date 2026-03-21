using Microsoft.Win32;
using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Notification;
using RevoltUltimate.API.Objects;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RevoltUltimate.Desktop.Options
{
    public partial class AchievementOptionsPage : UserControl
    {
        private readonly NotificationManager _notificationManager;
        private ObservableCollection<NotificationPluginViewModel> _plugins;

        public AchievementOptionsPage()
        {
            InitializeComponent();
            string notificationFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Notifications");
            _notificationManager = new NotificationManager(notificationFolder);

            LoadPlugins();
        }

        private void LoadPlugins()
        {
            _plugins = new ObservableCollection<NotificationPluginViewModel>();
            var installed = _notificationManager.GetInstalledPlugins();

            var currentActiveDllPath = App.Settings?.CustomAnimationDllPath ?? string.Empty;

            foreach (var plugin in installed)
            {
                var vm = new NotificationPluginViewModel(plugin);

                if (!string.IsNullOrEmpty(currentActiveDllPath) &&
                    plugin.EntryDllPath.Equals(currentActiveDllPath, StringComparison.OrdinalIgnoreCase))
                {
                    vm.IsSelected = true;
                }

                _plugins.Add(vm);
            }

            ThemeItemsControl.ItemsSource = _plugins;
            EmptyStateText.Visibility = _plugins.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Revolt Notification Themes (*.revolt)|*.revolt|ZIP Archives (*.zip)|*.zip|All files (*.*)|*.*",
                Title = "Install Notification Theme (.revolt)"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var newPlugin = _notificationManager.InstallFromRevoltArchive(openFileDialog.FileName);
                    LoadPlugins();
                    SetActivePlugin(newPlugin.EntryDllPath);
                    MessageBox.Show($"Theme '{newPlugin.Manifest.Name}' installed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to install theme: {ex.Message}", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SelectTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NotificationPluginViewModel vm)
            {
                SetActivePlugin(vm.Plugin.EntryDllPath);
            }
        }

        private void SetActivePlugin(string entryDllPath)
        {
            foreach (var vm in _plugins)
            {
                vm.IsSelected = vm.Plugin.EntryDllPath.Equals(entryDllPath, StringComparison.OrdinalIgnoreCase);
            }

            if (App.Settings != null)
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                if (entryDllPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    entryDllPath = entryDllPath.Substring(basePath.Length).TrimStart('\\', '/');
                }

                App.Settings.CustomAnimationDllPath = entryDllPath;
                SaveSettings();
            }
        }

        private void TestTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NotificationPluginViewModel vm)
            {
                TestTheme(vm.Plugin.EntryDllPath);
            }
        }

        private void TestTheme(string dllPath)
        {
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
            {
                MessageBox.Show($"The expected entry DLL was not found inside the mod package:\n{dllPath}", "DLL Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var testAchievement = new Achievement(
                Name: "Theme Selected",
                Description: "Your achievements will now look like this!",
                ImageUrl: "pack://application:,,,/RevoltUltimate.Desktop;component/Images/profilepic.png",
                Hidden: false,
                Id: 999,
                Unlocked: true,
                DateTimeUnlocked: DateTime.Now.ToString("o"),
                Difficulty: 30, // Random XP amount
                apiName: "Test_Achievement_UI",
                progress: false,
                currentProgress: 1,
                maxProgress: 1,
                getglobalpercentage: 100.0f
            );

            // Execute the payload DLL using your generic interface
            AchievementWindow.ShowNotification(testAchievement, dllPath);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPlugins();
        }

        private void SaveSettings()
        {
            try
            {
                string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "settings.json");
                if (App.Settings != null)
                {
                    File.WriteAllText(settingsFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(App.Settings, Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch { }
        }
        public void PromptInstallExternalTheme(string revoltPackagePath)
        {
            if (!File.Exists(revoltPackagePath)) return;

            var result = MessageBox.Show(
                $"Do you want to install the custom theme package:\n\n{Path.GetFileName(revoltPackagePath)}?",
                "Install Revolt Theme",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var newPlugin = _notificationManager.InstallFromRevoltArchive(revoltPackagePath);
                    LoadPlugins(); // Refresh

                    SetActivePlugin(newPlugin.EntryDllPath);
                    MessageBox.Show($"Theme '{newPlugin.Manifest.Name}' activated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to install theme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (e.Uri != null)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            e.Handled = true;
        }

        private void DeleteTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NotificationPluginViewModel vm)
            {
                if (vm.Manifest.Id == "default-xbox-toast")
                {
                    MessageBox.Show("This is the default system theme and cannot be deleted.", "Protected File", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to permanently delete '{vm.Manifest.Name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (vm.IsSelected)
                        {
                            SetActivePlugin("Notifications/default-xbox-toast/RevoltUltimate.Notification.dll");
                        }

                        if (Directory.Exists(vm.Plugin.FolderPath))
                        {
                            Directory.Delete(vm.Plugin.FolderPath, true);
                        }
                        LoadPlugins();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not delete folder. Make sure the file is not currently in use.\n{ex.Message}", "Error Deleting", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    public class NotificationPluginViewModel : INotifyPropertyChanged
    {
        public NotificationPluginInfo Plugin { get; }
        public string PreviewImagePath => Plugin.PreviewImagePath;
        public NotificationManifest Manifest => Plugin.Manifest;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(SelectButtonText));
                    OnPropertyChanged(nameof(SelectButtonBackground));
                }
            }
        }

        public string SelectButtonText => IsSelected ? "Selected" : "Select";
        public SolidColorBrush SelectButtonBackground => IsSelected ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : new SolidColorBrush(Color.FromRgb(63, 63, 70));

        public NotificationPluginViewModel(NotificationPluginInfo plugin)
        {
            Plugin = plugin;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
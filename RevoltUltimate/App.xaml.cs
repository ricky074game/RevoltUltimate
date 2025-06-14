using RevoltUltimate.API.Objects;
using RevoltUltimate.Setup;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace RevoltUltimate.Desktop
{
    public partial class App : Application
    {
        public static User? CurrentUser { get; set; }
        private static string SettingsFilePath => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "settings.json");

        private static string UserFilePath => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "user.json");
        private const string SetupCompleteEventName = "RevoltUltimateSetupCompleteEvent";
        public static ApplicationSettings? Settings { get; private set; }
        private const string CurrentSettingsVersion = "0.1";


        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadUser();
            if (CurrentUser == null)
            {
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                EventWaitHandle? setupCompleteEvent = null;
                try
                {
                    if (!EventWaitHandle.TryOpenExisting(SetupCompleteEventName, out setupCompleteEvent))
                    {
                        setupCompleteEvent = new EventWaitHandle(false, EventResetMode.ManualReset, SetupCompleteEventName);
                    }
                    else
                    {
                        setupCompleteEvent.Reset();
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error initializing setup event: {ex.Message}. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }

                try
                {
                    SetupWindow setupWindow = new SetupWindow();
                    setupWindow.Show();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Failed to start the setup process: {ex.Message}. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }

                bool signaled = false;
                if (setupCompleteEvent != null)
                {
                    await Task.Run(() =>
                    {
                        signaled = setupCompleteEvent.WaitOne(TimeSpan.FromMinutes(5));
                    });
                    setupCompleteEvent.Close();

                    if (!signaled)
                    {
                        MessageBox.Show("Setup process did not complete in time. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Current.Shutdown();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Setup completion event was not properly initialized. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }

                LoadUser();
                if (CurrentUser == null)
                {
                    MessageBox.Show("Setup was not completed successfully or user data is missing. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }

            }

            UpdateSettings();
            if (Settings == null)
            {
                Settings = new ApplicationSettings
                {
                    Version = CurrentSettingsVersion,
                    CustomAnimationDllPath = string.Empty,
                };
            }
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void UpdateSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    Settings = JsonSerializer.Deserialize<ApplicationSettings>(json);

                    if (Settings != null && Settings.Version == CurrentSettingsVersion)
                    {
                    }
                    else if (Settings != null && Settings.Version != CurrentSettingsVersion)
                    {
                        MessageBox.Show($"Settings file is an outdated version ({Settings.Version}). Resetting to default settings for version {CurrentSettingsVersion}.", "Settings Update", MessageBoxButton.OK, MessageBoxImage.Information);
                        Settings = null;
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"The settings file is corrupted. You can delete it (a new one will be generated on the next application start) or close the application to attempt a manual fix. For this session, default settings will be loaded.\nDetails: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Settings = null;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred while loading settings: {ex.Message}. Resetting to default settings.", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Settings = null;
                }
            }
        }
        private void LoadUser()
        {
            if (File.Exists(UserFilePath))
            {
                try
                {
                    string json = File.ReadAllText(UserFilePath);
                    CurrentUser = JsonSerializer.Deserialize<User>(json);
                }
                catch (JsonException)
                {
                    CurrentUser = null;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentUser = null;
                }
            }
            else
            {
                CurrentUser = null;
            }
        }
        public static void SaveUser(User user)
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(UserFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                string json = JsonSerializer.Serialize(user);
                File.WriteAllText(UserFilePath, json);
                CurrentUser = user;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to save user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
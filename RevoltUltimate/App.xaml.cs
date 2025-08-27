using Newtonsoft.Json;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Setup;
using RevoltUltimate.Desktop.Setup.Steam;
using System.IO;
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
            var bootScreen = new BootScreen();
            bootScreen.Show();

            try
            {
                LoadUser();
                if (CurrentUser == null)
                {
                    await RunSetup(bootScreen);
                }

                UpdateSettings();
                if (Settings == null)
                {
                    MessageBox.Show("Settings file is missing or corrupted. The application will now open setup.", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await RunSetup(bootScreen);
                }

                await SetupLinkers();

                bootScreen.Close();
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during startup: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                bootScreen.Close();
                Current.Shutdown();
            }
        }
        private void UpdateSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);

                    Settings = JsonConvert.DeserializeObject<ApplicationSettings>(json);

                    if (Settings != null && Settings.Version == CurrentSettingsVersion)
                    {
                        System.Diagnostics.Debug.WriteLine("Settings loaded successfully.");
                    }
                    else if (Settings != null && Settings.Version != CurrentSettingsVersion)
                    {
                        MessageBox.Show($"Settings file is an outdated version ({Settings.Version}). Resetting to default settings for version {CurrentSettingsVersion}.",
                            "Settings Update", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetToDefaultSettings();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                    MessageBox.Show($"An unexpected error occurred while loading settings: {ex.Message}. Resetting to default settings.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetToDefaultSettings();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Settings file not found. Resetting to default settings.");
                ResetToDefaultSettings();
            }
        }

        private void ResetToDefaultSettings()
        {
            Settings = new ApplicationSettings
            {
                Version = CurrentSettingsVersion,
                SteamApiKey = null,
                SteamId = null,
                CustomAnimationDllPath = null
            };

            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(SettingsFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (Settings != null)
                {
                    string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                    File.WriteAllText(SettingsFilePath, json);
                    System.Diagnostics.Debug.WriteLine("Settings saved successfully.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task RunSetup(BootScreen bootScreen)
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing setup event: {ex.Message}. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                bootScreen.Close();
                Current.Shutdown();
                return;
            }

            try
            {
                SetupWindow setupWindow = new SetupWindow();
                setupWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start the setup process: {ex.Message}. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                bootScreen.Close();
                Current.Shutdown();
                return;
            }

            bool signaled = false;
            await Task.Run(() =>
            {
                signaled = setupCompleteEvent.WaitOne(TimeSpan.FromMinutes(5));
            });
            setupCompleteEvent.Close();

            if (!signaled)
            {
                MessageBox.Show("Setup process did not complete in time. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                bootScreen.Close();
                Current.Shutdown();
                return;
            }

            LoadUser();
            if (CurrentUser == null)
            {
                MessageBox.Show("Setup was not completed successfully or user data is missing. Application will exit.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                bootScreen.Close();
                Current.Shutdown();
                return;
            }
        }

        private async Task SetupLinkers()
        {
            if (Settings != null && !string.IsNullOrEmpty(Settings.SteamApiKey) && !string.IsNullOrWhiteSpace(Settings.SteamId))
            {
                try
                {
                    SteamWeb.InitializeSharedInstance(Settings.SteamApiKey, Settings.SteamId);
                    System.Diagnostics.Debug.WriteLine("SteamWeb has been successfully initialized.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to initialize SteamWeb: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                }
            }

            var savedAccount = AccountManager.GetSavedAccounts().FirstOrDefault();
            if (savedAccount == null) return;

            var authenticator = new WpfAuthenticator(this.Dispatcher);
            SteamLocal.Instance.SetAuthenticator(authenticator);

            var result = await SteamLocal.Instance.Login(savedAccount.Username);

            if (result == SteamKit2.EResult.OK)
            {
                System.Diagnostics.Debug.WriteLine($"Successfully logged in as {savedAccount.Username}.");
            }
            else
            {
                MessageBox.Show($"Failed to log in as {savedAccount.Username}. Result: {result}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
        private void LoadUser()
        {
            if (File.Exists(UserFilePath))
            {
                try
                {
                    string json = File.ReadAllText(UserFilePath);

                    CurrentUser = JsonConvert.DeserializeObject<User>(json);

                    if (CurrentUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"User data loaded successfully for user: {CurrentUser.UserName}.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User data file exists but could not be deserialized. Resetting CurrentUser to null.");
                        CurrentUser = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading user data: {ex.Message}");
                    MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentUser = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User data file not found. Resetting CurrentUser to null.");
                CurrentUser = null;
            }
        }

    }
}
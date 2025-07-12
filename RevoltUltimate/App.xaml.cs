using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using RevoltUltimate.Desktop.Setup;
using System.IO;
using System.Windows;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.Desktop.Setup.Steam;

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
                MessageBox.Show("Settings file is missing or corrupted. The application will now open setup.", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                SetupWindow setupWindow = new SetupWindow();
                setupWindow.Show();
                return;
            }

            SetupLinkers();
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private async void SetupLinkers()
        {

            if (Settings != null || !string.IsNullOrEmpty(Settings.SteamApiKey) ||
                !string.IsNullOrWhiteSpace(Settings.SteamId))
            {
                try
                {
                    SteamWeb.InitializeSharedInstance(Settings.SteamApiKey, Settings.SteamId);
                    System.Diagnostics.Debug.WriteLine("SteamWeb has been successfully initialized.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to initialize SteamWeb: {ex.Message}", "Initialization Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                //Login failed
                MessageBox.Show($"Failed to log in as {savedAccount.Username}. Result: {result}", "Login Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                    }
                    else if (Settings != null && Settings.Version != CurrentSettingsVersion)
                    {
                        MessageBox.Show($"Settings file is an outdated version ({Settings.Version}). Resetting to default settings for version {CurrentSettingsVersion}.", "Settings Update", MessageBoxButton.OK, MessageBoxImage.Information);
                        Settings = null;
                    }
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
                    CurrentUser = JsonConvert.DeserializeObject<User>(json);
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
                string json = JsonConvert.SerializeObject(user, Formatting.Indented);
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
using RevoltUltimate.API.Objects;
using RevoltUltimate.Setup.Pages;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace RevoltUltimate.Setup
{
    public partial class SetupWindow : Window
    {
        private const string SetupCompleteEventName = "RevoltUltimateSetupCompleteEvent";

        public SetupWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new WelcomePage(this));
        }

        public void CompleteSetupAndLaunchMainApplication(User newUser)
        {

            SaveUserInSetup(newUser);

            try
            {
                if (EventWaitHandle.TryOpenExisting(SetupCompleteEventName, out EventWaitHandle? setupCompleteEvent))
                {
                    setupCompleteEvent.Set();
                    setupCompleteEvent.Close();
                }
                else
                {
                    MessageBox.Show("Error signaling setup completion: Event handle could not be opened.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error signaling setup completion: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.Close();
        }

        private void SaveUserInSetup(User user)
        {
            try
            {
                string userFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RevoltUltimate", "user.json");
                string? directoryPath = Path.GetDirectoryName(userFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = JsonSerializer.Serialize(user);
                File.WriteAllText(userFilePath, json);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to save user data during setup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
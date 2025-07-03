using System.Windows;
using SteamKit2.Authentication;
using System.Windows.Threading;

namespace RevoltUltimate.Desktop.Setup.Steam
{
    public class WpfAuthenticator : IAuthenticator
    {
        private readonly Dispatcher _dispatcher;

        // Event to notify the UI that a confirmation window needs to be shown
        public event Action<Window> OnDeviceConfirmationRequired;

        public WpfAuthenticator(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            _dispatcher.Invoke(() =>
            {
                // Create the window but let the UI layer manage showing and closing it.
                var confirmationWindow = new DeviceConfirmationWindow();
                OnDeviceConfirmationRequired?.Invoke(confirmationWindow);
            });

            // Return true to tell SteamKit to start polling for the user's mobile confirmation.
            return Task.FromResult(true);
        }

        public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            var tcs = new TaskCompletionSource<string>();

            _dispatcher.Invoke(() =>
            {
                var title = "Steam Guard Authentication";
                var message = previousCodeWasIncorrect
                    ? "The previous code was incorrect. Please enter the new code."
                    : "Please enter the Steam Guard code from your email or mobile authenticator.";

                var fields = new List<InputFieldDefinition>
                {
                    new InputFieldDefinition
                    {
                        Label = "DeviceCode",
                        DefaultValue = "Enter Steam Guard Code",
                        Validator = value => !string.IsNullOrWhiteSpace(value),
                        ValidationErrorMessage = "Email code is required."
                    }
                };

                var inputDialog = new InputDialog(message, fields, null) // Help URL is optional
                {
                    Title = title
                };

                if (inputDialog.ShowDialog() == true)
                {
                    if (inputDialog.Responses.TryGetValue("DeviceCode", out var deviceCode))
                    {
                        tcs.SetResult(deviceCode);
                    }
                    else
                    {
                        tcs.SetException(new KeyNotFoundException("DeviceCode was not found in the dialog responses."));
                    }
                }
                else
                {
                    tcs.SetCanceled();
                }
            });

            return tcs.Task;
        }

        public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            var tcs = new TaskCompletionSource<string>();

            _dispatcher.Invoke(() =>
            {
                var title = "Steam Email Authentication";
                var message = $"A confirmation code has been sent to your email at {email}. Please enter it below.";

                if (previousCodeWasIncorrect)
                {
                    message = $"The previous code was incorrect. {message}";
                }

                var fields = new List<InputFieldDefinition>
                {
                    new InputFieldDefinition
                    {
                        Label = "EmailCode",
                        DefaultValue = "Enter Email Code",
                        Validator = value => !string.IsNullOrWhiteSpace(value),
                        ValidationErrorMessage = "Email code is required."
                    }
                };

                var inputDialog = new InputDialog(message, fields, null)
                {
                    Title = title
                };

                if (inputDialog.ShowDialog() == true)
                {
                    if (inputDialog.Responses.TryGetValue("EmailCode", out var emailCode))
                    {
                        tcs.SetResult(emailCode);
                    }
                    else
                    {
                        tcs.SetException(new KeyNotFoundException("EmailCode was not found in the dialog responses."));
                    }
                }
                else
                {
                    tcs.SetCanceled();
                }
            });

            return tcs.Task;
        }
    }
}

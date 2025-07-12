using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Contracts;
using RevoltUltimate.API.Objects;
using SteamKit2;
using SteamKit2.Authentication;
using System.IO;
using System.Windows.Threading;
using static SteamKit2.Internal.CMsgRemoteClientBroadcastStatus;

namespace RevoltUltimate.API.Searcher
{
    public class SteamLocal
    {
        private static SteamLocal _instance;
        public static SteamLocal Instance => _instance ??= new SteamLocal();
        private readonly SteamClient _steamClient;
        private readonly CallbackManager _callbackManager;
        private readonly SteamUser _steamUser;
        private readonly SteamApps _steamApps;
        private IAuthenticator _authenticator;

        private bool _isRunning;
        private string _username;
        private string _password;
        private string _guardData;

        private readonly List<uint> _ownedAppIds = new List<uint>();
        private TaskCompletionSource<EResult> _loginTcs;

        public SteamLocal()
        {
            _steamClient = new SteamClient();
            _callbackManager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamApps = _steamClient.GetHandler<SteamApps>();

            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            _callbackManager.Subscribe<SteamApps.LicenseListCallback>(OnLicenseList);
        }
        public void SetAuthenticator(IAuthenticator authenticator)
        {
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        }

        public Task<EResult> Login(string username, string? password = null)
        {
            _username = username;
            _password = password ?? AccountManager.GetDecryptedPassword(username);
            _loginTcs = new TaskCompletionSource<EResult>();

            _guardData = AccountManager.GetDecryptedGuardData(_username);

            _isRunning = true;
            _steamClient.Connect();

            Task.Run(() =>
            {
                while (_isRunning)
                {
                    _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
            });

            return _loginTcs.Task;
        }
        public void Disconnect()
        {
            _isRunning = false;
            _steamUser.LogOff();
            _steamClient.Disconnect();
        }

        private async void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine($"Connected to Steam! Authenticating '{_username}'...");

            try
            {
                var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = _username,
                    Password = _password,
                    IsPersistentSession = true,
                    GuardData = _guardData,
                    Authenticator = _authenticator,
                });

                var pollResponse = await authSession.PollingWaitForResultAsync();

                if (pollResponse.NewGuardData != null)
                {
                    _guardData = pollResponse.NewGuardData;
                    AccountManager.SaveAccount(_username, _guardData, _password);
                }

                _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                    ShouldRememberPassword = true,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
                _loginTcs.TrySetResult(EResult.Fail);
            }
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected from Steam.");
            _loginTcs?.TrySetResult(EResult.NoConnection);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            Console.WriteLine(callback.Result == EResult.OK
                ? "Successfully logged on!"
                : $"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");

            _loginTcs.TrySetResult(callback.Result);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine($"Logged off of Steam: {callback.Result}");
        }

        private void OnLicenseList(SteamApps.LicenseListCallback callback)
        {
            if (callback.Result != EResult.OK) return;
            _ownedAppIds.Clear();
            _ownedAppIds.AddRange(callback.LicenseList.Select(lic => lic.PackageID));
        }


        public async Task<List<Game>> GetOwnedGamesAsync()
        {
            if (!_steamClient.IsConnected || !_ownedAppIds.Any())
            {
                throw new InvalidOperationException("Not connected to Steam or no game licenses found.");
            }

            var games = new List<Game>();
            var requests = _ownedAppIds.Distinct().Select(appId => new SteamApps.PICSRequest(appId)).ToList();
            var productInfo = await _steamApps.PICSGetProductInfo(requests, new List<SteamApps.PICSRequest>()).ToTask();

            foreach (var app in productInfo.Results.SelectMany(r => r.Apps.Values))
            {
                var type = app.KeyValues["common"]["type"]?.AsString()?.ToLower();
                if (type != "game")
                    continue;

                var name = app.KeyValues["common"]["name"]?.AsString();
                if (string.IsNullOrEmpty(name))
                    continue;

                var game = new Game(name, "Steam", "", "", "SteamKit2");
                games.Add(game);
            }

            return games;
        }
    }
}
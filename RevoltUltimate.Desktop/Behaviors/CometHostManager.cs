using RevoltUltimate.API.Comet;

namespace RevoltUltimate.Desktop.Comet
{
    public class CometHostManager : IAsyncDisposable
    {
        private readonly CometManager? _cometManager;
        private readonly string? _username;
        private readonly bool _isDebugMode;
        private readonly bool _isServiceConnectionNeeded;

        private CometHostManager(CometManager? cometManager, string? username, bool isDebugMode, bool isServiceConnectionNeeded)
        {
            _cometManager = cometManager;
            _username = username;
            _isDebugMode = isDebugMode;
            _isServiceConnectionNeeded = isServiceConnectionNeeded;
        }

        public static async Task<CometHostManager> CreateAsync()
        {
            var manager = new CometHostManager(
                App.CometManager,
                App.CurrentUser?.UserName,
                App.IsDebugMode,
                App.CometManager?.Service.IsConnected ?? false
            );
            await manager.StopCometAsync();
            return manager;
        }

        private async Task StopCometAsync()
        {
            if (_cometManager != null)
            {
                await Task.Run(() => _cometManager.Stop());
            }
        }

        private async Task RestartCometAsync()
        {
            if (_cometManager != null)
            {
                await Task.Run(() => _cometManager.Start(_username, _isDebugMode));
                if (_isServiceConnectionNeeded)
                {
                    await _cometManager.Service.StartAsync(new System.Threading.CancellationToken());
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await RestartCometAsync();
        }
    }
}
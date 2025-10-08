using RevoltUltimate.API.Native;
using System.Diagnostics;
using System.IO;

namespace RevoltUltimate.API.Comet
{
    public class CometManager : IDisposable
    {
        private Process? _cometProcess;
        private JobManager? _jobManager;

        public event Action<string?>? CometLogReceived;
        public event Action<string?>? CometErrorLogReceived;

        public CometService Service { get; } = new();

        public void Start(string? username, bool isDebugMode)
        {
            const string cometProcessName = "comet";
            string cometExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comet", $"{cometProcessName}.exe");

            if (!File.Exists(cometExePath))
            {
                CometErrorLogReceived?.Invoke($"Comet executable not found at: {cometExePath}");
                return;
            }

            if (Process.GetProcessesByName(cometProcessName).Length > 0)
            {
                CometErrorLogReceived?.Invoke("Comet process is already running.");
                return;
            }

            var startInfo = new ProcessStartInfo(cometExePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(cometExePath)
            };

            var arguments = new List<string>();
            if (!string.IsNullOrEmpty(username))
            {
                arguments.Add($"--username {username}");
            }
            if (isDebugMode)
            {
                arguments.Add("-d");
            }
            startInfo.Arguments = string.Join(" ", arguments);

            try
            {
                _cometProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                _cometProcess.OutputDataReceived += (sender, args) => CometLogReceived?.Invoke(args.Data);
                _cometProcess.ErrorDataReceived += (sender, args) => CometErrorLogReceived?.Invoke(args.Data);
                _cometProcess.Start();
                _cometProcess.BeginOutputReadLine();
                _cometProcess.BeginErrorReadLine();

                _jobManager = new JobManager();
                _jobManager.AddProcess(Process.GetCurrentProcess());
                _jobManager.AddProcess(_cometProcess);

                CometLogReceived?.Invoke($"Started Comet process with arguments: {startInfo.Arguments}");
            }
            catch (Exception ex)
            {
                CometErrorLogReceived?.Invoke($"Failed to start Comet process: {ex.Message}");
            }
        }

        public void Stop()
        {
            Service.Stop();
            try
            {
                if (_cometProcess is { HasExited: false })
                {
                    _cometProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                CometErrorLogReceived?.Invoke($"Failed to kill Comet process: {ex.Message}");
            }
            finally
            {
                _jobManager?.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevoltUltimate.API.Objects;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RevoltUltimate.API.Comet
{
    public class CometService
    {
        private const string Host = "127.0.0.1";
        private const int Port = 3333;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cancellationTokenSource;

        public event Action<GameConnectedData>? GameConnected;
        public event Action<AchievementUnlockedData>? AchievementUnlocked;

        public bool IsConnected => _client?.Connected ?? false;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(Host, Port);
                _stream = _client.GetStream();

                Trace.WriteLine("CometService: Connected to cometoffline.");

                var initialMessage = new { status = "success", message = "RevoltUltimate connected" };
                await SendJsonAsync(initialMessage);

                _ = Task.Run(() => ListenForMessages(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
            catch (SocketException ex)
            {
                Trace.WriteLine($"CometService: Failed to connect. Is cometoffline running? Details: {ex.Message}");
                Stop();
            }
        }

        private async Task ListenForMessages(CancellationToken token)
        {
            if (_stream == null) return;
            var reader = new StreamReader(_stream, Encoding.UTF8);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(token);
                    if (line == null)
                    {
                        Trace.WriteLine("CometService: Connection closed by server.");
                        break;
                    }

                    ProcessMessage(line);
                }
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("CometService: Listening was canceled.");
            }
            catch (IOException ex)
            {
                Trace.WriteLine($"CometService: I/O error while listening: {ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"CometService: An unexpected error occurred while listening: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private void ProcessMessage(string json)
        {
            try
            {
                Trace.WriteLine($"CometService: Received message: {json}");
                var message = JObject.Parse(json);
                var type = message["type"]?.Value<string>();
                var data = message["data"];

                if (data == null) return;

                switch (type)
                {
                    case "game_connected":
                        var gameInfo = data.ToObject<GameConnectedData>();
                        if (gameInfo != null)
                        {
                            GameConnected?.Invoke(gameInfo);
                        }
                        break;
                    case "achievement_unlocked":
                        var achievementData = data.ToObject<AchievementUnlockedData>();
                        if (achievementData != null)
                        {
                            AchievementUnlocked?.Invoke(achievementData);
                        }
                        break;
                    default:
                        Trace.WriteLine($"CometService: Received unknown message type '{type}'.");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Trace.WriteLine($"CometService: Error deserializing message: {ex.Message}");
            }
        }

        public async Task SendJsonAsync(object data)
        {
            if (!IsConnected || _stream == null)
            {
                Trace.WriteLine("CometService: Cannot send message, not connected.");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(data);
                string payload = json + "\n"; // Use newline as a delimiter
                byte[] buffer = Encoding.UTF8.GetBytes(payload);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
                Trace.WriteLine($"CometService: Sent message: {json}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"CometService: Error sending message: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            _stream?.Close();
            _client?.Close();
            _cancellationTokenSource?.Dispose();
            Trace.WriteLine("CometService: Connection stopped.");
        }
    }
}
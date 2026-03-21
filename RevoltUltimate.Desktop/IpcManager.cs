using System.IO.Pipes;
using System.IO;

namespace RevoltUltimate.Desktop
{
    public static class IpcManager
    {
        private const string PipeName = "RevoltUltimateSingleInstancePipe";
        public static event Action<string>? OnFileReceived;

        public static bool SendArgsToExistingInstance(string[] args)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                // Attempt to connect quickly. If it fails, there is no main instance.
                client.Connect(300);
                using var writer = new StreamWriter(client);
                writer.WriteLine(string.Join("|", args));
                writer.Flush();
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void StartListening()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                        await server.WaitForConnectionAsync();

                        using var reader = new StreamReader(server);
                        var message = await reader.ReadLineAsync();

                        if (!string.IsNullOrEmpty(message))
                        {
                            var args = message.Split('|');
                            if (args.Length > 0 && args[0].EndsWith(".revolt", StringComparison.OrdinalIgnoreCase))
                            {
                                OnFileReceived?.Invoke(args[0]);
                            }
                        }
                    }
                    catch { /* Keep listening on error */ }
                }
            });
        }
    }
}
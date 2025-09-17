using System.IO;
using System.Reflection;

namespace RevoltUltimate.API.Fetcher
{
    public static class ApiKeyManager
    {
        public static string LoadApiKey(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Could not find embedded resource: '{resourceName}'. Ensure the file exists, its Build Action is set to 'Embedded Resource', and the resource name is correct.");
            }
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }

        public static (string ClientId, string ClientSecret) LoadTwitchCredentials(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Could not find embedded resource: '{resourceName}'. Ensure the file exists, its Build Action is set to 'Embedded Resource', and the resource name is correct.");
            }
            using StreamReader reader = new StreamReader(stream);
            string? clientId = reader.ReadLine()?.Trim();
            string? clientSecret = reader.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException($"Could not read credentials from '{resourceName}'. Ensure the file contains the Client ID on the first line and the Client Secret on the second line.");
            }

            return (clientId, clientSecret);
        }
    }
}
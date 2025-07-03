using Newtonsoft.Json;

namespace RevoltUltimate.Desktop
{
    public class ApplicationSettings
    {
        public string Version { get; set; } = "0.1";

        [JsonIgnore]
        public string? CustomAnimationDllPath { get; set; }

        public string? SteamApiKey { get; set; }
        public string? SteamId { get; set; }

        public ApplicationSettings()
        {
            if (string.IsNullOrEmpty(Version))
            {
                Version = "0.1";
            }
        }
    }
}
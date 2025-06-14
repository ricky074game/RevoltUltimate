using System.Text.Json.Serialization;

namespace RevoltUltimate.Setup
{
    public class ApplicationSettings
    {
        public string Version { get; set; } = "0.1"; // Default version
        public string? SteamApiKey { get; set; }
        public string? SteamId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CustomAnimationDllPath { get; set; }


        public ApplicationSettings()
        {

        }
    }
}
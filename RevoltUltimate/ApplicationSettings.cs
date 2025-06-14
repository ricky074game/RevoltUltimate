using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RevoltUltimate.Desktop
{
    public class ApplicationSettings
    {
        public String Version { get; set; } = "0.1";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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

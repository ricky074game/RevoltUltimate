using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RevoltUltimate.API.Update
{
    public class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/ricky074game/RevoltUltimate/releases/latest";

        public async Task<string> GetLatestVersionAsync(string currentVersion)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "RevoltUltimate");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(GitHubApiUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return currentVersion;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release != null && !string.IsNullOrEmpty(release.TagName))
            {
                string latestVersion = release.TagName.TrimStart('v', 'V');
                return latestVersion;
            }

            return currentVersion;
        }

        public static bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            currentVersion = currentVersion.TrimStart('v', 'V');
            latestVersion = latestVersion.TrimStart('v', 'V');

            if (Version.TryParse(currentVersion, out var current) &&
                Version.TryParse(latestVersion, out var latest))
            {
                return latest > current;
            }

            return string.Compare(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase) < 0;
        }

        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }
        }
    }
}
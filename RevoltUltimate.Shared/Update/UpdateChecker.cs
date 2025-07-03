using System.Net.Http;
using System.Text.Json;

namespace RevoltUltimate.API.Update
{
    public class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/ricky074game/RevoltUltimate/releases/latest";

        public async Task<string> GetLatestVersionAsync(string currentVersion)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "RevoltUltimate");

                try
                {
                    var response = await client.GetStringAsync(GitHubApiUrl);
                    var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                    if (release != null && !string.IsNullOrEmpty(release.TagName))
                    {
                        if (IsNewerVersion(currentVersion, release.TagName))
                        {
                            return $"New version available: {release.TagName}. Download at {release.HtmlUrl}";
                        }
                        else
                        {
                            return "You are using the latest version.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Error checking for updates: {ex.Message}";
                }
            }

            return "Unable to check for updates.";
        }

        private bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            return string.Compare(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase) < 0;
        }

        private class GitHubRelease
        {
            public string TagName { get; set; }
            public string HtmlUrl { get; set; }
        }
    }
}
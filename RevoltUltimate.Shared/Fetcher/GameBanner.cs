using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RevoltUltimate.API.Fetcher
{
    public class BannerCacheEntry
    {
        public string GameName { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime CachedAt { get; set; }
    }

    public class GameBanner
    {
        private static readonly string ApiKey = LoadApiKey();
        private static readonly HttpClient HttpClient = new();
        private static readonly string CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache", "Banners");
        public string? Name { get; set; }

        public GameBanner(string gameName)
        {
            Name = gameName;
            Directory.CreateDirectory(CacheDirectory); // Ensure cache directory exists
        }
        private static string LoadApiKey()
        {
            var assembly = Assembly.GetExecutingAssembly();
            // The resource name is typically <AssemblyName>.<FolderPath>.<FileName>
            // Assuming your assembly is RevoltUltimate.Shared and the file is in the Fetcher folder:
            string resourceName = "RevoltUltimate.Shared.Fetcher.SteamGridDbApiKey.txt";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    // Fallback for cases where the namespace might be slightly different or project structure influences the name.
                    // This attempts a name based on the class's namespace.
                    string alternativeResourceName = typeof(GameBanner).Namespace + ".SteamGridDbApiKey.txt";
                    using (Stream? alternativeStream = assembly.GetManifestResourceStream(alternativeResourceName))
                    {
                        if (alternativeStream == null)
                        {
                            // If still not found, you might need to list all resources to find the correct name:
                            // string[] names = assembly.GetManifestResourceNames();
                            // Console.WriteLine("Available resources: " + string.Join(", ", names));
                            throw new InvalidOperationException($"Could not find embedded resource. Tried: '{resourceName}' and '{alternativeResourceName}'. Ensure 'SteamGridDbApiKey.txt' exists in the Fetcher folder, its Build Action is 'Embedded Resource', and the resource name is correct.");
                        }
                        using (StreamReader reader = new StreamReader(alternativeStream))
                        {
                            return reader.ReadToEnd().Trim();
                        }
                    }
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
        }

        private string GetCacheFilePath(string gameName)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(gameName.ToLowerInvariant()));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return Path.Combine(CacheDirectory, $"{hashString}.json");
        }

        private async Task<BannerCacheEntry?> LoadFromCacheAsync(string gameName)
        {
            string cacheFilePath = GetCacheFilePath(gameName);
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cacheFilePath);
                    var cacheEntry = JsonSerializer.Deserialize<BannerCacheEntry>(json);
                    return cacheEntry;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading cache for {gameName}: {ex.Message}");
                }
            }
            return null;
        }

        private async Task SaveToCacheAsync(string gameName, string? bannerUrl)
        {
            string cacheFilePath = GetCacheFilePath(gameName);
            var cacheEntry = new BannerCacheEntry
            {
                GameName = gameName,
                BannerUrl = bannerUrl,
                CachedAt = DateTime.UtcNow
            };
            try
            {
                var json = JsonSerializer.Serialize(cacheEntry, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing cache for {gameName}: {ex.Message}");
            }
        }

        public async Task<string?> GetGameBannerUrlAsync(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName)) return null;

            var cachedEntry = await LoadFromCacheAsync(gameName);
            if (cachedEntry != null)
            {
                Console.WriteLine($"Cache hit for {gameName}. Using cached URL: {cachedEntry.BannerUrl}");
                return cachedEntry.BannerUrl;
            }

            Console.WriteLine($"Cache miss for {gameName}. Fetching from API.");
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://www.steamgriddb.com/api/v2/search/autocomplete/{Uri.EscapeDataString(gameName)}"
                );
                request.Headers.Add("Authorization", $"Bearer {ApiKey}");
                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(content);
                var data = json.RootElement.GetProperty("data");
                if (data.GetArrayLength() > 0)
                {
                    var gameId = data[0].GetProperty("id").GetInt32();

                    var gridRequest = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"https://www.steamgriddb.com/api/v2/grids/game/{gameId}?types=static"
                    );
                    gridRequest.Headers.Add("Authorization", $"Bearer {ApiKey}");
                    var gridResponse = await HttpClient.SendAsync(gridRequest);
                    gridResponse.EnsureSuccessStatusCode();

                    var gridContent = await gridResponse.Content.ReadAsStringAsync();
                    using var gridJson = JsonDocument.Parse(gridContent);
                    var grids = gridJson.RootElement.GetProperty("data");
                    if (grids.GetArrayLength() > 0)
                    {
                        string? bannerUrl = grids[0].GetProperty("url").GetString();
                        if (!string.IsNullOrEmpty(bannerUrl))
                        {
                            await SaveToCacheAsync(gameName, bannerUrl);
                        }
                        return bannerUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching game banner from SteamGridDB for {gameName}: {ex.Message}");
            }
            return null;
        }
    }
}
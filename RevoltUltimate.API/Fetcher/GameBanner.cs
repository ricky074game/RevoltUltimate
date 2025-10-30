using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace RevoltUltimate.API.Fetcher
{
    public class BannerUrlCacheEntry
    {
        public string GameName { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime CachedAt { get; set; }
    }

    public class GameBanner
    {
        private static readonly string ApiKey = ApiKeyManager.LoadApiKey("RevoltUltimate.API.Fetcher.SteamGridDbApiKey.txt");
        private static readonly HttpClient HttpClient = new();
        private static readonly string UrlCacheDirectory = Path.Combine(Path.GetTempPath(), "RevoltUltimateUrlCache", "Banners");
        private static readonly string ImageCacheDirectory = Path.Combine(Path.GetTempPath(), "RevoltUltimateImageCache", "Banners");
        private static readonly SHA256 _sha256 = SHA256.Create();
        private static readonly object _sha256Lock = new object();
        public string? Name { get; set; }

        public GameBanner(string gameName)
        {
            Name = gameName;
            Directory.CreateDirectory(UrlCacheDirectory);
            Directory.CreateDirectory(ImageCacheDirectory);
        }

        private string GetUrlCacheFilePath(string gameName)
        {
            byte[] hashBytes;
            lock (_sha256Lock)
            {
                hashBytes = _sha256.ComputeHash(Encoding.UTF8.GetBytes(gameName.ToLowerInvariant()));
            }
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return Path.Combine(UrlCacheDirectory, $"{hashString}.json");
        }

        private async Task<BannerUrlCacheEntry?> LoadUrlFromCacheAsync(string gameName)
        {
            string cacheFilePath = GetUrlCacheFilePath(gameName);
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cacheFilePath);
                    var cacheEntry = JsonConvert.DeserializeObject<BannerUrlCacheEntry>(json);
                    if ((DateTime.UtcNow - cacheEntry.CachedAt).TotalDays < 7)
                    {
                        return cacheEntry;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error reading URL cache for {gameName}: {ex.Message}");
                }
            }
            return null;
        }

        private async Task SaveUrlToCacheAsync(string gameName, string? bannerUrl)
        {
            string cacheFilePath = GetUrlCacheFilePath(gameName);
            var cacheEntry = new BannerUrlCacheEntry
            {
                GameName = gameName,
                BannerUrl = bannerUrl,
                CachedAt = DateTime.UtcNow
            };
            try
            {
                var json = JsonConvert.SerializeObject(cacheEntry, Formatting.Indented);
                await File.WriteAllTextAsync(cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error writing URL cache for {gameName}: {ex.Message}");
            }
        }

        private string GetImageCachePath(string bannerUrl)
        {
            var uri = new Uri(bannerUrl);
            string fileName = Path.GetFileName(uri.LocalPath);
            return Path.Combine(ImageCacheDirectory, fileName);
        }

        private async Task DownloadImageAsync(string bannerUrl, string imageCachePath)
        {
            try
            {
                var imageBytes = await HttpClient.GetByteArrayAsync(bannerUrl);
                await File.WriteAllBytesAsync(imageCachePath, imageBytes);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to download or save image {bannerUrl}: {ex.Message}");
            }
        }

        public async Task<string?> GetBannerImagePathAsync(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName)) return null;

            var cachedUrlEntry = await LoadUrlFromCacheAsync(gameName);
            string? bannerUrl = cachedUrlEntry?.BannerUrl;

            if (bannerUrl == null)
            {
                Trace.WriteLine($"Cache miss for {gameName} URL. Fetching from API.");
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
                    var json = JsonConvert.DeserializeObject<dynamic>(content);
                    var data = json["data"] as JArray;
                    if (data != null && data.Count > 0)
                    {
                        var gameId = data[0]["id"];

                        var gridRequest = new HttpRequestMessage(
                            HttpMethod.Get,
                            $"https://www.steamgriddb.com/api/v2/grids/game/{gameId}?types=static"
                        );
                        gridRequest.Headers.Add("Authorization", $"Bearer {ApiKey}");
                        var gridResponse = await HttpClient.SendAsync(gridRequest);
                        gridResponse.EnsureSuccessStatusCode();

                        var gridContent = await gridResponse.Content.ReadAsStringAsync();
                        var gridJson = JsonConvert.DeserializeObject<dynamic>(gridContent);
                        data = gridJson["data"] as JArray;
                        if (data != null && data.Count > 0)
                        {
                            bannerUrl = data[0]["url"]?.ToString();
                            if (!string.IsNullOrEmpty(bannerUrl))
                            {
                                await SaveUrlToCacheAsync(gameName, bannerUrl);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error fetching game banner from SteamGridDB for {gameName}: {ex.Message}");
                    return null;
                }
            }

            if (string.IsNullOrEmpty(bannerUrl))
            {
                return null;
            }

            string imageCachePath = GetImageCachePath(bannerUrl);
            if (!File.Exists(imageCachePath))
            {
                await DownloadImageAsync(bannerUrl, imageCachePath);
            }

            return File.Exists(imageCachePath) ? imageCachePath : null;
        }
    }
}
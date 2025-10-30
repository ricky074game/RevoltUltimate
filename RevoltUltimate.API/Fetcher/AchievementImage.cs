using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Net.Http;

namespace RevoltUltimate.API.Fetcher
{
    public class AchievementImage
    {
        private readonly string _cacheDirectory;
        private static readonly HttpClient _httpClient = new HttpClient();

        public AchievementImage()
        {
            _cacheDirectory = Path.Combine(Path.GetTempPath(), "RevoltUltimateImageCache");
            EnsureCacheDirectoryExists();
        }

        private void EnsureCacheDirectoryExists()
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<Image<Rgba32>?> GetProcessedImageAsync(string imageUrl, bool unlocked, bool hidden)
        {
            string cacheFilePath;

            if (File.Exists(imageUrl))
            {
                cacheFilePath = imageUrl;
            }
            else
            {
                cacheFilePath = GetCachedImagePath(imageUrl);

                if (!File.Exists(cacheFilePath))
                {
                    await DownloadImageAsync(imageUrl, cacheFilePath).ConfigureAwait(false);
                }
            }

            return ProcessImage(cacheFilePath, unlocked, hidden);
        }

        private string GetCachedImagePath(string imageUrl)
        {
            string fileName = Path.GetFileName(imageUrl);
            return Path.Combine(_cacheDirectory, fileName);
        }

        private async Task DownloadImageAsync(string imageUrl, string cacheFilePath)
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
                await File.WriteAllBytesAsync(cacheFilePath, imageBytes).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading image from {imageUrl}: {ex.Message}");
            }
        }

        private Image<Rgba32> ProcessImage(string filePath, bool unlocked, bool hidden)
        {
            var image = Image.Load<Rgba32>(filePath);

            if (!unlocked)
            {
                image.Mutate(x => x.Grayscale());
            }

            if (!unlocked && hidden)
            {
                image.Mutate(x => x.GaussianBlur(5));
            }

            return image;
        }

        public void CleanupCache()
        {
            var files = Directory.GetFiles(_cacheDirectory);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastAccessTime < DateTime.Now.AddDays(-7))
                {
                    fileInfo.Delete();
                }
            }
        }
    }
}

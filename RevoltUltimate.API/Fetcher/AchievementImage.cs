using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Net;

namespace RevoltUltimate.API.Fetcher
{
    public class AchievementImage
    {
        private readonly string _cacheDirectory;

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

        public Image<Rgba32>? GetProcessedImage(string imageUrl, bool unlocked, bool hidden)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return CreateBlankWhiteImage();
            }

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
                    try
                    {
                        DownloadImage(imageUrl, cacheFilePath);
                    }
                    catch (WebException)
                    {
                        return CreateBlankWhiteImage();
                    }
                }
            }

            return ProcessImage(cacheFilePath, unlocked, hidden);
        }
        private Image<Rgba32> CreateBlankWhiteImage(int width = 64, int height = 64)
        {
            return new Image<Rgba32>(width, height, Color.White);
        }

        private string GetCachedImagePath(string imageUrl)
        {
            string fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            return Path.Combine(_cacheDirectory, fileName);
        }

        private void DownloadImage(string imageUrl, string cacheFilePath)
        {
            using var webClient = new System.Net.WebClient();
            webClient.DownloadFile(imageUrl, cacheFilePath);
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
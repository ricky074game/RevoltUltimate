using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace RevoltUltimate.API.Notification
{
    public class NotificationPluginInfo
    {
        public NotificationManifest Manifest { get; set; } = new();
        public string FolderPath { get; set; } = string.Empty;
        public string EntryDllPath => Path.Combine(FolderPath, Manifest.Entry);
        public string PreviewImagePath => !string.IsNullOrEmpty(Manifest.PreviewImage)
            ? Path.Combine(FolderPath, Manifest.PreviewImage)
            : string.Empty;
    }

    public class NotificationManager
    {
        private string BasePath { get; }

        public NotificationManager(string basePath)
        {
            BasePath = basePath;
            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }
        }

        public NotificationPluginInfo InstallFromRevoltArchive(string archivePath)
        {
            if (!File.Exists(archivePath) || !archivePath.EndsWith(".revolt", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The file must be a valid .revolt package.");

            string tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(archivePath, tempExtractPath);

            string manifestPath = Path.Combine(tempExtractPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Directory.Delete(tempExtractPath, true);
                throw new Exception("Invalid .revolt package. Missing manifest.json");
            }

            string manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<NotificationManifest>(manifestJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (manifest == null || string.IsNullOrEmpty(manifest.Id))
            {
                Directory.Delete(tempExtractPath, true);
                throw new Exception("Invalid manifest data or missing ID.");
            }

            string destFolder = Path.Combine(BasePath, manifest.Id);
            if (Directory.Exists(destFolder))
            {
                Directory.Delete(destFolder, true); // Overwrite if it already exists
            }

            Directory.Move(tempExtractPath, destFolder);

            return new NotificationPluginInfo { Manifest = manifest, FolderPath = destFolder };
        }

        public List<NotificationPluginInfo> GetInstalledPlugins()
        {
            var plugins = new List<NotificationPluginInfo>();

            foreach (var dir in Directory.GetDirectories(BasePath))
            {
                var manifestFile = Path.Combine(dir, "manifest.json");
                if (File.Exists(manifestFile))
                {
                    try
                    {
                        var manifest = JsonSerializer.Deserialize<NotificationManifest>(File.ReadAllText(manifestFile), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (manifest != null)
                        {
                            plugins.Add(new NotificationPluginInfo
                            {
                                Manifest = manifest,
                                FolderPath = dir
                            });
                        }
                    }
                    catch { }
                }
            }
            return plugins;
        }
    }
}
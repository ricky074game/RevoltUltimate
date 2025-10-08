using Newtonsoft.Json;
using RevoltUltimate.API.Objects;
using System.Diagnostics;
using System.IO;

namespace RevoltUltimate.API.Comet
{
    public class AchievementProvider
    {
        private readonly string _achievementsRepoPath;

        public AchievementProvider()
        {
            _achievementsRepoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("RevoltAchievement", "Achievements"));
        }

        public async Task<Game?> GetGameAchievementsAsync(long gameId, string platform)
        {
            var filePath = Path.Combine(_achievementsRepoPath, platform, $"{gameId}.json");

            if (!File.Exists(filePath))
            {
                Trace.WriteLine($"AchievementProvider: Achievement file not found for game {gameId} on platform {platform} at {filePath}");
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var game = JsonConvert.DeserializeObject<Game>(json);
                return game;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"AchievementProvider: Error reading or deserializing achievement file for game {gameId}: {ex.Message}");
                return null;
            }
        }
    }
}
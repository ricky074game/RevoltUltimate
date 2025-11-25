using System.Diagnostics;
using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;
using System.Text.RegularExpressions;

namespace RevoltUltimate.API.Update
{
    public class SteamLocalUpdate : Update
    {
        public override async Task<List<Achievement>> CheckForNewAchievementsAsync(Game game)
        {
            var newlyEarned = new List<Achievement>();

            uint appId = 0;
            if (!string.IsNullOrEmpty(game.description))
            {
                var match = Regex.Match(game.description, @"Steam App (\d+)");
                if (match.Success && uint.TryParse(match.Groups[1].Value, out uint parsedAppId))
                {
                    appId = parsedAppId;
                }
            }

            if (appId == 0)
            {
                Trace.WriteLine($"Could not find a valid AppId for game: {game.name}");
                return newlyEarned;
            }

            var latestAchievements = await SteamScrape.Instance.GetPlayerAchievementsAsync(appId);

            if (latestAchievements != null)
            {
                var existingAchievements = game.achievements
                    .GroupBy(a => a.name)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var latestAchievement in latestAchievements)
                {
                    if (existingAchievements.TryGetValue(latestAchievement.name, out var existingAchievement))
                    {
                        if (latestAchievement.unlocked && !existingAchievement.unlocked)
                        {
                            newlyEarned.Add(latestAchievement);
                            existingAchievement.SetUnlockedStatus(true, latestAchievement.datetimeunlocked);
                        }
                    }
                    else
                    {
                        game.AddAchievement(latestAchievement);
                        if (latestAchievement.unlocked)
                        {
                            newlyEarned.Add(latestAchievement);
                        }
                    }
                }
            }

            return newlyEarned;
        }
    }
}
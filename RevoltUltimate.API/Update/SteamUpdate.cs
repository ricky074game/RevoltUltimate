using RevoltUltimate.API.Objects;
using RevoltUltimate.API.Searcher;

namespace RevoltUltimate.API.Update
{
    public class SteamUpdate : Update
    {
        public override async Task<List<Achievement>> CheckForNewAchievementsAsync(Game game)
        {
            if (!SteamWeb.Instance.IsSteamApiReady)
            {
                return new List<Achievement>();
            }

            var newlyEarned = new List<Achievement>();
            var latestAchievements = await SteamWeb.Instance.GetAchievementsForGame(game.name);

            if (latestAchievements != null)
            {
                var existingAchievements = game.achievements.ToDictionary(a => a.apiName);

                foreach (var latestAchievement in latestAchievements)
                {
                    if (existingAchievements.TryGetValue(latestAchievement.apiName, out var existingAchievement))
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
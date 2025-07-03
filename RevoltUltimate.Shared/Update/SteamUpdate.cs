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

            var newAchievements = new List<Achievement>();
            var latestAchievements = await SteamWeb.Instance.GetAchievementsForGame(game.name);

            if (latestAchievements != null)
            {
                var existingAchievementNames = new HashSet<string>(game.achievements.Select(a => a.name));
                foreach (var latestAchievement in latestAchievements)
                {
                    if (!existingAchievementNames.Contains(latestAchievement.name))
                    {
                        newAchievements.Add(latestAchievement);
                        game.AddAchievement(latestAchievement);
                    }
                }
            }

            return newAchievements;
        }
    }
}

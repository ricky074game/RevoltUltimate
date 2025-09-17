using RevoltUltimate.API.Objects;

namespace RevoltUltimate.API.Update
{
    public abstract class Update
    {
        public abstract Task<List<Achievement>> CheckForNewAchievementsAsync(Game game);
    }
}

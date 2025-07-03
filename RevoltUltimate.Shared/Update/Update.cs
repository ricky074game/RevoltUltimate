using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RevoltUltimate.API.Objects;

namespace RevoltUltimate.API.Update
{
    public abstract class Update
    {
        public abstract Task<List<Achievement>> CheckForNewAchievementsAsync(Game game);
    }
}

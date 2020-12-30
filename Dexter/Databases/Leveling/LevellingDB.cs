using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Leveling {
    
    public class LevellingDB {

        public DbSet<VoiceLevel> VoiceLevels { get; set; }

    }

}

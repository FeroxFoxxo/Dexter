using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Cooldowns {
    
    public class CooldownDB : EntityDatabase {
        
        public DbSet<Cooldown> Cooldowns { get; set; }

    }

}

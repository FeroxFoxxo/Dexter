using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Cooldowns {
    
    public class CooldownDB : EntityDatabase {
        
        public DbSet<Cooldown> CommissionCooldowns { get; set; }

        public DbSet<Cooldown> FunCooldowns { get; set; }

    }

}

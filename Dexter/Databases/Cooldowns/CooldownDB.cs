using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Cooldowns {
    
    /// <summary>
    /// The database which stores all data related to maintaining and processing command cooldowns.
    /// </summary>

    public class CooldownDB : Database {
        
        /// <summary>
        /// The set of all managed cooldowns in the corresponding database.
        /// </summary>

        public DbSet<Cooldown> Cooldowns { get; set; }

    }

}

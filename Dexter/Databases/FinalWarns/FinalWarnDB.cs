using Dexter.Abstractions;
using Dexter.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.FinalWarns
{
    /// <summary>
    /// A database which holds a list of final warns, both historical and active, and keeps relevant information about them.
    /// </summary>

    public class FinalWarnDB : Database {

        /// <summary>
        /// The set of final warns stored in the database, accessed by the ID of the user who has been warned.
        /// </summary>

        public DbSet<FinalWarn> FinalWarns { get; set; }

        /// <summary>
        /// Represents the configured settings attached to the Moderation module.
        /// </summary>

        public ModerationConfiguration ModerationConfiguration { get; set; }

    }
}

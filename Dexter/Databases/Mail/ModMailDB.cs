using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Mail {

    /// <summary>
    /// The ModMailDB contains a set of messages sent to the moderators.
    /// </summary>

    public class ModMailDB : Database {

        /// <summary>
        /// A table of the sent modmail messages.
        /// </summary>

        public DbSet<ModMail> ModMail { get; set; }

    }

}

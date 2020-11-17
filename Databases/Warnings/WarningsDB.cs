using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.Warnings {

    /// <summary>
    /// The WarningsDB contains a set of warnings, issued by a moderator, when a user has been warned in chat.
    /// </summary>
    public class WarningsDB : EntityDatabase {

        /// <summary>
        /// A table of the warnings issued in the WarningDB database.
        /// </summary>
        public DbSet<Warning> Warnings { get; set; }

        /// <summary>
        /// The Get Warnings method queries the database for all warnings that have not been revoked
        /// based on the warned user's snowflake ID.
        /// </summary>
        /// <param name="UserID">The ID of the user you wish to query the warnings of.</param>
        /// <returns>An array of warning objects that the user has.</returns>
        public Warning[] GetWarnings(ulong UserID) =>
            Warnings.AsQueryable()
            .Where(Warning => Warning.User == UserID && Warning.EntryType != EntryType.Removed)
            .ToArray();

    }

}

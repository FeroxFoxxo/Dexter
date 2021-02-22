using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Borkdays {

    /// <summary>
    /// The BorkdayDB contains a set of all the users who have had the borkday role and the time of the issue.
    /// </summary>

    public class BorkdayDB : Database {

        /// <summary>
        /// A table of the borkday times in the BorkdayDB database.
        /// </summary>

        public DbSet<Borkday> Borkdays { get; set; }

    }

}

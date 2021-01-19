using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.EventTimers {

    /// <summary>
    /// The database which stores all data related to maintaining and processing event timers.
    /// </summary>

    public class EventTimersDB : Database {

        /// <summary>
        /// The set of all managed event timers in the corresponding database.
        /// </summary>

        public DbSet<EventTimer> EventTimers { get; set; }

    }

}

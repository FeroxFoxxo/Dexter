using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.CommunityEvents {

    /// <summary>
    /// Holds and manages the events suggested by members of the community for approval, modification, and release.
    /// </summary>

    public class CommunityEventsDB : Database {

        /// <summary>
        /// Holds every individual event that has been suggested into the system.
        /// </summary>

        public DbSet<CommunityEvent> Events { get; set; }

        private int Count = 1;

        /// <summary>
        /// Generates the lowest available token above a local counter used for optimization.
        /// </summary>
        /// <returns>A unique integer that can be used as a key for a new Event in the Events database.</returns>

        public int GenerateToken() {
            while (Events.Find(Count) != null) Count++;
            return Count;
        }
    }
}

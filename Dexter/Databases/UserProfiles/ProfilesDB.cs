using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.UserProfiles {

    /// <summary>
    /// The BorkdayDB contains a set of all the users who have had the borkday role and the time of the issue.
    /// </summary>

    public class ProfilesDB : Database {

        /// <summary>
        /// A table of the borkday times in the BorkdayDB database.
        /// </summary>

        public DbSet<UserProfile> Profiles { get; set; }

        /// <summary>
        /// Holds all recorded nickname and username changes.
        /// </summary>

        public DbSet<NameRecord> Names { get; set; }

        /// <summary>
        /// Fetches a profile from the database or creates a new one if none exists for the given UserID (filled with default values).
        /// </summary>
        /// <param name="UserID">The ID of the profile to fetch.</param>
        /// <returns>A UserProfile object detailing the relevant information about the user.</returns>

        public UserProfile GetOrCreateProfile(ulong UserID) {
            UserProfile Profile = Profiles.Find(UserID);

            if (Profile is null) {
                Profile = new() { UserID = UserID };

                Profiles.Add(Profile);
                SaveChanges();
            }

            return Profile;
        }

    }

}

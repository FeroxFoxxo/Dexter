using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.Infractions {

    /// <summary>
    /// The InfractionsDB contains a set of warnings, issued by a moderator, when a user has been warned in chat.
    /// </summary>
    
    public class InfractionsDB : Database {

        /// <summary>
        /// A table of the warnings issued in the InfractionsDB database.
        /// </summary>

        public DbSet<Infraction> Infractions { get; set; }

        public DbSet<DexterProfile> DexterProfiles { get; set; }

        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// The Get Infractions method queries the database for all infractions that have not been revoked
        /// based on the warned user's snowflake ID.
        /// </summary>
        /// <param name="UserID">The ID of the user you wish to query the infractions of.</param>
        /// <returns>An array of infraction objects that the user has.</returns>

        public Infraction[] GetInfractions(ulong UserID) =>
            Infractions.AsQueryable()
            .Where(Warning => Warning.User == UserID && Warning.EntryType != EntryType.Revoke)
            .ToArray();

        public DexterProfile GetOrCreateProfile(ulong UserID) {
            DexterProfile DexterProfile = DexterProfiles.Find(UserID);

            if (DexterProfile == null) {
                DexterProfile = new DexterProfile() { UserID = UserID, InfractionAmount = ModerationConfiguration.MaxPoints, CurrentMute = "", CurrentPointTimer = "" };
                DexterProfiles.Add(DexterProfile);
                SaveChanges();
            }

            return DexterProfile;
        }

    }

}

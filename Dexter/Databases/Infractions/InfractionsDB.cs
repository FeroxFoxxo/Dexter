using System.Linq;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Infractions
{

    /// <summary>
    /// The InfractionsDB contains a set of warnings, issued by a moderator, when a user has been warned in chat.
    /// </summary>

    public class InfractionsDB : Database
    {

        /// <summary>
        /// A table of the warnings issued in the InfractionsDB database.
        /// </summary>

        public DbSet<Infraction> Infractions { get; set; }

        /// <summary>
        /// The set of Dexter Profiles in the corresponding database.
        /// </summary>

        public DbSet<DexterProfile> DexterProfiles { get; set; }

        /// <summary>
        /// Represents the configured settings attached to the Moderation module.
        /// </summary>

        private ModerationConfiguration ModerationConfiguration;

        public InfractionsDB(ModerationConfiguration moderationConfiguration)
        {
            ModerationConfiguration = moderationConfiguration;
        }

        /// <summary>
        /// The Get Infractions method queries the database for all infractions that have not been revoked
        /// based on the warned user's snowflake ID.
        /// </summary>
        /// <param name="user">The ID of the user you wish to query the infractions of.</param>
        /// <returns>An array of infraction objects that the user has.</returns>

        public Infraction[] GetInfractions(ulong user) =>
            Infractions.AsQueryable()
            .Where(Warning => Warning.User == user && Warning.EntryType != EntryType.Revoke)
            .ToArray();

        /// <summary>
        /// Gets a user's assigned Dexter profile if it exists. Otherwise, it creates a new profile and links it to them for further use.
        /// </summary>
        /// <param name="user">The user attached to the target Dexter Profile.</param>
        /// <returns>The corresponding Dexter Profile of the selected user.</returns>

        public DexterProfile GetOrCreateProfile(ulong user)
        {
            DexterProfile dexterProfile = DexterProfiles.Find(user);

            if (dexterProfile == null)
            {
                dexterProfile = new DexterProfile() { UserID = user, InfractionAmount = ModerationConfiguration.MaxPoints, CurrentMute = "", CurrentPointTimer = "" };
                DexterProfiles.Add(dexterProfile);
            }

            return dexterProfile;
        }


    }

}

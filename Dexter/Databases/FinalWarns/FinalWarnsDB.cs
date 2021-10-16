using System;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.FinalWarns
{
    /// <summary>
    /// A database which holds a list of final warns, both historical and active, and keeps relevant information about them.
    /// </summary>

    public class FinalWarnsDB : Database
    {

        /// <summary>
        /// The set of final warns stored in the database, accessed by the ID of the user who has been warned.
        /// </summary>

        public DbSet<FinalWarn> FinalWarns { get; set; }

        /// <summary>
        /// Represents the configured settings attached to the Moderation module.
        /// </summary>

        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// Checks whether an active final warn is logged for <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to query in the database.</param>
        /// <returns><see langword="true"/> if the user has an active final warn; <see langword="false"/> otherwise.</returns>

        public bool IsUserFinalWarned(IGuildUser user)
        {
            FinalWarn warn = FinalWarns.Find(user.Id);

            return warn != null && warn.EntryType == EntryType.Issue;
        }

        /// <summary>
        /// Looks for a final warn entry for the target <paramref name="user"/>. If none are found, it creates one. If one is found, it is overwritten by the new parameters.
        /// </summary>
        /// <param name="deducted">The amounts of points deducted from the <paramref name="user"/>'s profile when the final warn was issued.</param>
        /// <param name="issuer">The staff member who issued the final warning.</param>
        /// <param name="user">The user who is to receive a final warn.</param>
        /// <param name="duration">The duration of the mute attached to the final warn.</param>
        /// <param name="reason">The whole reason behind the final warn for <paramref name="user"/>.</param>
        /// <param name="msgID">The ID of the message within #final-warnings which records this final warn instance.</param>
        /// <returns>The <c>FinalWarn</c> object added to the database.</returns>

        public FinalWarn SetOrCreateFinalWarn(short deducted, IGuildUser issuer, IGuildUser user, TimeSpan duration, string reason, ulong msgID)
        {
            FinalWarn warning = FinalWarns.Find(user.Id);

            FinalWarn newWarning = new()
            {
                IssuerID = issuer.Id,
                UserID = user.Id,
                MuteDuration = duration.TotalSeconds,
                Reason = reason,
                EntryType = EntryType.Issue,
                IssueTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                MessageID = msgID,
                PointsDeducted = deducted
            };

            if (warning == null)
            {
                FinalWarns.Add(newWarning);
            }
            else
            {
                FinalWarns.Remove(warning);
                FinalWarns.Add(newWarning);
            }

            return newWarning;
        }

        /// <summary>
        /// Sets the status of a <paramref name="user"/>'s final warn to Revoked, but doesn't remove it from the database.
        /// </summary>
        /// <param name="user">The user whose final warn is to be revoked.</param>
        /// <param name="warning">The warn found in the database, or <see langword="null"/> if no warn is found.</param>
        /// <returns><see langword="true"/> if an active final warn was found for the <paramref name="user"/>, whose status was changed to revoked; otherwise <see langword="false"/>.</returns>

        public bool TryRevokeFinalWarn(IGuildUser user, out FinalWarn warning)
        {
            warning = FinalWarns.Find(user.Id);

            if (warning == null || warning.EntryType == EntryType.Revoke) return false;

            warning.EntryType = EntryType.Revoke;

            return true;
        }

        /// <summary>
        /// Sets the status of a <paramref name="user"/>'s final warn to Revoked, but doesn't remove it from the database.
        /// </summary>
        /// <param name="user">The user whose final warn is to be revoked.</param>
        /// <returns><see langword="true"/> if an active final warn was found for the <paramref name="user"/>, whose status was changed to revoked; otherwise <see langword="false"/>.</returns>

        public bool TryRevokeFinalWarn(IGuildUser user)
        {
            return TryRevokeFinalWarn(user, out _);
        }

    }
}

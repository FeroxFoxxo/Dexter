using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Microsoft.EntityFrameworkCore;
using System;

namespace Dexter.Databases.FinalWarns
{
    /// <summary>
    /// A database which holds a list of final warns, both historical and active, and keeps relevant information about them.
    /// </summary>

    public class FinalWarnsDB : Database {

        /// <summary>
        /// The set of final warns stored in the database, accessed by the ID of the user who has been warned.
        /// </summary>

        public DbSet<FinalWarn> FinalWarns { get; set; }

        /// <summary>
        /// Represents the configured settings attached to the Moderation module.
        /// </summary>

        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// Checks whether an active final warn is logged for <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The user to query in the database.</param>
        /// <returns><see langword="true"/> if the user has an active final warn; <see langword="false"/> otherwise.</returns>

        public bool IsUserFinalWarned(IGuildUser User) {
            FinalWarn Warn = FinalWarns.Find(User.Id);

            return Warn != null && Warn.EntryType == EntryType.Issue;
        }

        /// <summary>
        /// Looks for a final warn entry for the target <paramref name="User"/>. If none are found, it creates one. If one is found, it is overwritten by the new parameters.
        /// </summary>
        /// <param name="PointsDeducted">The amounts of points deducted from the <paramref name="User"/>'s profile when the final warn was issued.</param>
        /// <param name="Issuer">The staff member who issued the final warning.</param>
        /// <param name="User">The user who is to receive a final warn.</param>
        /// <param name="MuteDuration">The duration of the mute attached to the final warn.</param>
        /// <param name="Reason">The whole reason behind the final warn for <paramref name="User"/>.</param>
        /// <param name="MessageID">The ID of the message within #final-warnings which records this final warn instance.</param>
        /// <returns>The <c>FinalWarn</c> object added to the database.</returns>

        public FinalWarn SetOrCreateFinalWarn(short PointsDeducted, IGuildUser Issuer, IGuildUser User, TimeSpan MuteDuration, string Reason, ulong MessageID) {
            FinalWarn Warn = FinalWarns.Find(User.Id);

            FinalWarn NewWarn = new FinalWarn() {
                IssuerID = Issuer.Id,
                UserID = User.Id,
                MuteDuration = MuteDuration.TotalSeconds,
                Reason = Reason,
                EntryType = EntryType.Issue,
                IssueTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                MessageID = MessageID,
                PointsDeducted = PointsDeducted
            };

            if (Warn == null) {
                FinalWarns.Add(NewWarn);
            } else {
                FinalWarns.Remove(Warn);
                FinalWarns.Add(NewWarn);
            }

            SaveChanges();

            return NewWarn;
        }

        /// <summary>
        /// Sets the status of a <paramref name="User"/>'s final warn to Revoked, but doesn't remove it from the database.
        /// </summary>
        /// <param name="User">The user whose final warn is to be revoked.</param>
        /// <param name="Warn">The warn found in the database, or <see langword="null"/> if no warn is found.</param>
        /// <returns><see langword="true"/> if an active final warn was found for the <paramref name="User"/>, whose status was changed to revoked; otherwise <see langword="false"/>.</returns>

        public bool TryRevokeFinalWarn(IGuildUser User, out FinalWarn Warn) {
            Warn = FinalWarns.Find(User.Id);

            if (Warn == null || Warn.EntryType == EntryType.Revoke) return false;

            Warn.EntryType = EntryType.Revoke;

            SaveChanges();
            return true;
        }

        /// <summary>
        /// Sets the status of a <paramref name="User"/>'s final warn to Revoked, but doesn't remove it from the database.
        /// </summary>
        /// <param name="User">The user whose final warn is to be revoked.</param>
        /// <returns><see langword="true"/> if an active final warn was found for the <paramref name="User"/>, whose status was changed to revoked; otherwise <see langword="false"/>.</returns>

        public bool TryRevokeFinalWarn(IGuildUser User) {
            return TryRevokeFinalWarn(User, out _);
        }

    }
}

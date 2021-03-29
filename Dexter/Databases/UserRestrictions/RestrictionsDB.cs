using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.UserRestrictions {

    /// <summary>
    /// Stores, retrieves, and manages the collection of user-specific restrictions applied for command or feature usage.
    /// </summary>

    public class RestrictionsDB : Database {

        /// <summary>
        /// A collection of user-specific restrictions.
        /// </summary>

        public DbSet<UserRestriction> UserRestrictions { get; set; }

        /// <summary>
        /// Gets all restrictions related to a user, or NONE if the <paramref name="UserID"/> is not in the database.
        /// </summary>
        /// <param name="UserID">The ID of the target user to fetch from the database.</param>
        /// <returns>.</returns>

        public Restriction GetUserRestrictions(ulong UserID) {
            UserRestriction UR = UserRestrictions.Find(UserID);

            if (UR == null) return Restriction.None;

            return UR.RestrictionFlags;
        }

        /// <summary>
        /// Gets all restrictions related to a <paramref name="User"/>, or NONE if the user's ID is not in the database.
        /// </summary>
        /// <param name="User">The User to fetch from the database.</param>
        /// <returns>A Restriction object representing all flags for which the user is restricted.</returns>

        public Restriction GetUserRestrictions(Discord.IUser User) {
            return GetUserRestrictions(User.Id);
        }

        /// <summary>
        /// Checks whether a given <paramref name="UserID"/> which represents a user who has a set of restrictions <paramref name="Restriction"/>.
        /// </summary>
        /// <remarks>
        ///     <para>If <paramref name="MatchAny"/> is <see langword="true"/>, it will return <see langword="true"/> if the user has ANY of the restrictions flagged by <paramref name="Restriction"/>.</para>
        ///     <para>If <paramref name="MatchAny"/> is <see langword="false"/>, it will only return <see langword="true"/> if the user has ALL flagged restrictions.</para>
        /// </remarks>
        /// <param name="UserID">The Id of the target user to query the database for.</param>
        /// <param name="Restriction">The individual or multiple restriction(s) to check for.</param>
        /// <param name="MatchAny">Dictates the matching mode, if set to <see langword="true"/>, the matching becomes non-strict.</param>
        /// <returns><see langword="true"/> if the user has all restriction flags in <paramref name="Restriction"/>, otherwise <see langword="false"/>.</returns>

        public bool IsUserRestricted(ulong UserID, Restriction Restriction, bool MatchAny = false) {
            if (MatchAny) return (GetUserRestrictions(UserID) & Restriction) != Restriction.None;
            else return (GetUserRestrictions(UserID) & Restriction) == Restriction;
        }

        /// <summary>
        /// Checks whether a given <paramref name="User"/> has a set of restrictions <paramref name="Restriction"/>.
        /// </summary>
        /// <param name="User">The target user to query the database for.</param>
        /// <param name="Restriction">The individual or multiple restriction(s) to check for.</param>
        /// <returns><see langword="true"/> if the <paramref name="User"/> has all restriction flags in <paramref name="Restriction"/>, otherwise <see langword="false"/>.</returns>

        public bool IsUserRestricted(Discord.IUser User, Restriction Restriction) {
            return IsUserRestricted(User.Id, Restriction);
        }

        /// <summary>
        /// Adds a given <paramref name="Restriction"/> to a user's registry, and creates a new one if none exists for the given user by <paramref name="UserID"/>.
        /// </summary>
        /// <param name="UserID">The ID of the target User.</param>
        /// <param name="Restriction">The Restriction flags to add to the User.</param>
        /// <returns><see langword="false"/> if the user already had that <paramref name="Restriction"/>, otherwise <see langword="true"/>.</returns>

        public bool AddRestriction(ulong UserID, Restriction Restriction) {
            UserRestriction UR = UserRestrictions.Find(UserID);

            if (UR == null) {
                UR = new() {
                    UserID = UserID,
                    RestrictionFlags = Restriction
                };
                UserRestrictions.Add(UR);
                return true;
            }

            if ((UR.RestrictionFlags | Restriction) == UR.RestrictionFlags) return false;

            UR.RestrictionFlags |= Restriction;
            return true;
        }

        /// <summary>
        /// Adds a given <paramref name="Restriction"/> to a user's registry, and creates a new one if none exists for the given <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The target User.</param>
        /// <param name="Restriction">The Restriction flags to add to <paramref name="User"/>.</param>
        /// <returns><see langword="false"/> if the user already had that <paramref name="Restriction"/>, otherwise <see langword="true"/>.</returns>

        public bool AddRestriction(Discord.IUser User, Restriction Restriction) {
            return AddRestriction(User.Id, Restriction);
        }

        /// <summary>
        /// Removes a set of restrictions <paramref name="Restriction"/> from a user given by <paramref name="UserID"/>.
        /// </summary>
        /// <param name="UserID">The target user's unique ID.</param>
        /// <param name="Restriction">The Restriction flags to remove from User.</param>
        /// <returns><see langword="true"/> if the restriction was removed, <see langword="false"/> if the user wasn't in the database or didn't have that restriction.</returns>

        public bool RemoveRestriction(ulong UserID, Restriction Restriction) {
            UserRestriction UR = UserRestrictions.Find(UserID);

            if (UR == null) return false;

            if ((UR.RestrictionFlags & Restriction) == Restriction.None) return false;

            UR.RestrictionFlags &= ~Restriction;

            return true;
        }

        /// <summary>
        /// Removes a set of restrictions <paramref name="Restriction"/> from a target <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The target user.</param>
        /// <param name="Restriction">The Restriction flags to remove from <paramref name="User"/>.</param>
        /// <returns><see langword="true"/> if the restriction was removed, <see langword="false"/> if the user wasn't in the database or didn't have that restriction.</returns>

        public bool RemoveRestriction(Discord.IUser User, Restriction Restriction) {
            return RemoveRestriction(User.Id, Restriction);
        }

    }
}

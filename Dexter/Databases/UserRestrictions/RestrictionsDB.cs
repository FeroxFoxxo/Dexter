using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.UserRestrictions
{

    /// <summary>
    /// Stores, retrieves, and manages the collection of user-specific restrictions applied for command or feature usage.
    /// </summary>

    public class RestrictionsDB : Database
    {

        /// <summary>
        /// A collection of user-specific restrictions.
        /// </summary>

        public DbSet<UserRestriction> UserRestrictions { get; set; }

        /// <summary>
        /// Gets all restrictions related to a user, or NONE if the <paramref name="userID"/> is not in the database.
        /// </summary>
        /// <param name="userID">The ID of the target user to fetch from the database.</param>
        /// <returns>A Restriction object representing all flags for which the user is restricted.</returns>

        public Restriction GetUserRestrictions(ulong userID)
        {
            UserRestriction userRestriction = UserRestrictions.Find(userID);

            if (userRestriction == null) return Restriction.None;

            return userRestriction.RestrictionFlags;
        }

        /// <summary>
        /// Gets all restrictions related to a <paramref name="user"/>, or NONE if the user's ID is not in the database.
        /// </summary>
        /// <param name="user">The User to fetch from the database.</param>
        /// <returns>A Restriction object representing all flags for which the user is restricted.</returns>

        public Restriction GetUserRestrictions(Discord.IUser user)
        {
            return GetUserRestrictions(user.Id);
        }

        /// <summary>
        /// Checks whether a given <paramref name="userID"/> which represents a user who has a set of restrictions <paramref name="restriction"/>.
        /// </summary>
        /// <remarks>
        ///     <para>If <paramref name="matchAny"/> is <see langword="true"/>, it will return <see langword="true"/> if the user has ANY of the restrictions flagged by <paramref name="restriction"/>.</para>
        ///     <para>If <paramref name="matchAny"/> is <see langword="false"/>, it will only return <see langword="true"/> if the user has ALL flagged restrictions.</para>
        /// </remarks>
        /// <param name="userID">The Id of the target user to query the database for.</param>
        /// <param name="restriction">The individual or multiple restriction(s) to check for.</param>
        /// <param name="matchAny">Dictates the matching mode, if set to <see langword="true"/>, the matching becomes non-strict.</param>
        /// <returns><see langword="true"/> if the user has all restriction flags in <paramref name="restriction"/>, otherwise <see langword="false"/>.</returns>

        public bool IsUserRestricted(ulong userID, Restriction restriction, bool matchAny = false)
        {
            if (matchAny) return (GetUserRestrictions(userID) & restriction) != Restriction.None;
            else return (GetUserRestrictions(userID) & restriction) == restriction;
        }

        /// <summary>
        /// Checks whether a given <paramref name="user"/> has a set of restrictions <paramref name="restriction"/>.
        /// </summary>
        /// <param name="user">The target user to query the database for.</param>
        /// <param name="restriction">The individual or multiple restriction(s) to check for.</param>
        /// <returns><see langword="true"/> if the <paramref name="user"/> has all restriction flags in <paramref name="restriction"/>, otherwise <see langword="false"/>.</returns>

        public bool IsUserRestricted(Discord.IUser user, Restriction restriction)
        {
            return IsUserRestricted(user.Id, restriction);
        }

        /// <summary>
        /// Adds a given <paramref name="restriction"/> to a user's registry, and creates a new one if none exists for the given user by <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The ID of the target User.</param>
        /// <param name="restriction">The Restriction flags to add to the User.</param>
        /// <returns><see langword="false"/> if the user already had that <paramref name="restriction"/>, otherwise <see langword="true"/>.</returns>

        public bool AddRestriction(ulong userID, Restriction restriction)
        {
            UserRestriction userRestriction = UserRestrictions.Find(userID);

            if (userRestriction == null)
            {
                userRestriction = new()
                {
                    UserID = userID,
                    RestrictionFlags = restriction
                };
                UserRestrictions.Add(userRestriction);
                return true;
            }

            if ((userRestriction.RestrictionFlags | restriction) == userRestriction.RestrictionFlags) return false;

            userRestriction.RestrictionFlags |= restriction;
            return true;
        }

        /// <summary>
        /// Adds a given <paramref name="restriction"/> to a user's registry, and creates a new one if none exists for the given <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The target User.</param>
        /// <param name="restriction">The Restriction flags to add to <paramref name="user"/>.</param>
        /// <returns><see langword="false"/> if the user already had that <paramref name="restriction"/>, otherwise <see langword="true"/>.</returns>

        public bool AddRestriction(Discord.IUser user, Restriction restriction)
        {
            return AddRestriction(user.Id, restriction);
        }

        /// <summary>
        /// Removes a set of restrictions <paramref name="restriction"/> from a user given by <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The target user's unique ID.</param>
        /// <param name="restriction">The Restriction flags to remove from User.</param>
        /// <returns><see langword="true"/> if the restriction was removed, <see langword="false"/> if the user wasn't in the database or didn't have that restriction.</returns>

        public bool RemoveRestriction(ulong userID, Restriction restriction)
        {
            UserRestriction userRestriction = UserRestrictions.Find(userID);

            if (userRestriction == null) return false;

            if ((userRestriction.RestrictionFlags & restriction) == Restriction.None) return false;

            userRestriction.RestrictionFlags &= ~restriction;

            return true;
        }

        /// <summary>
        /// Removes a set of restrictions <paramref name="restriction"/> from a target <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The target user.</param>
        /// <param name="restriction">The Restriction flags to remove from <paramref name="user"/>.</param>
        /// <returns><see langword="true"/> if the restriction was removed, <see langword="false"/> if the user wasn't in the database or didn't have that restriction.</returns>

        public bool RemoveRestriction(Discord.IUser user, Restriction restriction)
        {
            return RemoveRestriction(user.Id, restriction);
        }

    }
}

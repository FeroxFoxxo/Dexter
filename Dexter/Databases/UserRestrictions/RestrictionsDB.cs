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

    }
}

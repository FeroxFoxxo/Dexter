using System;
namespace Dexter.Databases.UserRestrictions
{

    /// <summary>
    /// Represents a set of restrictions from different server features as a combination of flags.
    /// </summary>

    [Flags]
    public enum Restriction : ulong
    {
        /// <summary>
        /// Represents the null restriction (default)
        /// </summary>
        None = 0,
        /// <summary>
        /// Prevents users from sending private modmails.
        /// </summary>
        Modmail = 1,
        /// <summary>
        /// Prevents users from proposing events.
        /// </summary>
        Events = 2,
        /// <summary>
        /// Prevents users from making suggestions.
        /// </summary>
        Suggestions = 4,
        /// <summary>
        /// Prevents users from suggesting new topics.
        /// </summary>
        TopicManagement = 8,
        /// <summary>
        /// Prevents users from managing or joining games.
        /// </summary>
        Games = 16,
        /// <summary>
        /// Prevents users from viewing or editing their profiles, or anyone from accessing their profile.
        /// </summary>
        Social = 32,
        /// <summary>
        /// Prevents users from obtaining Voice XP altogether.
        /// </summary>
        VoiceXP = 64
    }
}

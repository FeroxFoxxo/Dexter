using System;

namespace Dexter.Databases.UserRestrictions {

    /// <summary>
    /// Represents a set of restrictions from different server features as a combination of flags.
    /// </summary>

    [Flags]
    public enum Restriction : ulong {
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
        TopicManagement = 8
    }
}

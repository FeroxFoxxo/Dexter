namespace Dexter.Databases.UserProfiles {

    /// <summary>
    /// Expresses whether a name record is a USERNAME or a NICKNAME.
    /// </summary>

    public enum NameType {
        /// <summary>
        /// Represents a discord tag (User's Username, Guild-insensitive)
        /// </summary>
        Username,

        /// <summary>
        /// Represents a guild-specific nickname.
        /// </summary>
        Nickname
    }
}

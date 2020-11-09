namespace Dexter.Enums {
    /// <summary>
    /// An enum of the types of the different permission levels a user can have.
    /// </summary>
    public enum PermissionLevel {

        /// <summary>
        /// The DEFAULT value is given to an ordinary user with no special roles.
        /// </summary>
        Default,

        /// <summary>
        /// The MODERATOR value is given to a user who hold the moderator role specified in the bot configuration.
        /// </summary>
        Moderator,

        /// <summary>
        /// The ADMINISTRATOR value is given to a user who holds the ADMINISTRATOR permission in the server.
        /// </summary>
        Administrator

    }
}

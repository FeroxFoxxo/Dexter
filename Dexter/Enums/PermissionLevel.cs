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
        /// The GREETFUR value is given to a user who hold the greetfur role specified in the bot configuration.
        /// </summary>

        GreetFur,

        /// <summary>
        /// The MODERATOR value is given to a user who holds the moderator role specified in the bot configuration.
        /// </summary>

        Moderator,

        /// <summary>
        /// The DEVELOPER value is given to a user who hold the development team role specified in the bot configuration.
        /// </summary>

        Developer,

        /// <summary>
        /// The ADMINISTRATOR value is given to a user who holds the administrator role specified in the bot configuration.
        /// </summary>

        Administrator

    }

}

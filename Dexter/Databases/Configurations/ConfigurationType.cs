namespace Dexter.Databases.Configurations {

    /// <summary>
    /// This enum specifies what state the module is in. Whether it be enabled or disabled, or essential to the usage of the bot.
    /// </summary>
    
    public enum ConfigurationType {

        /// <summary>
        /// The ESSENTIAL field specifies that a module has the essential module attribute.
        /// Essential modules will always be enabled, as they are vital for the use of the bot.
        /// </summary>
        
        Essential,

        /// <summary>
        /// The ENABLED field specifies that a module has been manually enabled by a user.
        /// All commands with this configuration type will run and are added to the CommandService.
        /// </summary>
        
        Enabled,

        /// <summary>
        /// The DISABLED field specifies that this module is not currently enabled in the database.
        /// All commands with this configuration will not run and have not been added to the CommandService.
        /// </summary>
        
        Disabled

    }

}

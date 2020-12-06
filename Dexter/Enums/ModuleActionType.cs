namespace Dexter.Enums {

    /// <summary>
    /// An enum of the types of actions you can run through the ~module command.
    /// </summary>
    
    public enum ModuleActionType {

        /// <summary>
        /// The ENABLE value will set the specified module to be enabled.
        /// </summary>
        
        Enable,

        /// <summary>
        /// The DISABLE value will set the specified module to be disabled.
        /// </summary>
        
        Disable,

        /// <summary>
        /// The STATUS value will query the database for the related status of the specified command
        /// </summary>
        
        Status

    }

}

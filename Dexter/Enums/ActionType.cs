namespace Dexter.Enums {

    /// <summary>
    /// The ActionType represents an action that a user may make on a command
    /// that interfaces with the fun database. It includes add, remove, get and edit commands.
    /// </summary>
    
    public enum ActionType {

        /// <summary>
        /// The UNKNOWN field specifies that the given command has not been specified.
        /// </summary>
        
        Unknown,

        /// <summary>
        /// The ADD field specifies the user is adding the given field to the database.
        /// </summary>
        
        Add,

        /// <summary>
        /// The REMOVE field specifies the user is removing the given field from the database.
        /// </summary>
        
        Remove,

        /// <summary>
        /// The EDIT field specifies the user is changing a given entry in the database.
        /// </summary>
        
        Edit,

        /// <summary>
        /// The GET field returns the ID of an entry from the database.
        /// </summary>
        
        Get,

        /// <summary>
        /// The DECLINE action is used to set a Proposal's status to declined.
        /// </summary>

        Decline,

        /// <summary>
        /// The APPROVE action is used to set a Proposal's status to approved.
        /// </summary>

        Approve

    }

}

namespace Dexter.Enums {

    /// <summary>
    /// The ActionType represents a generic action that a user may make on a command
    /// that interfaces with a database. It includes add, remove and edit commands.
    /// </summary>
    public enum ActionType {

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
        Edit

    }

}

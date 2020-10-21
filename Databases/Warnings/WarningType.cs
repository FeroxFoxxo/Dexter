namespace Dexter.Databases.Warnings {

    /// <summary>
    /// This enum is to be able to see whether or not this warning has been removed or not.
    /// If this warning has been revoked, it will be set to the REVOKED state - otherwise,
    /// it will have the ISSUED value state.
    /// </summary>
    public enum WarningType {

        /// <summary>
        /// The issued enum state is to differenciate this warning to one that has been revoked.
        /// When a warning is created, it is automatically set to this enum value unless a command
        /// has been run that has changed its state into a revoked state.
        /// </summary>
        Issued,

        /// <summary>
        /// Whenever a warning is revoked, either through ~revoke or ~purge (removing all warnings),
        /// it sets the warning to a revoked state. This is to help with accidental purges. When running
        /// the records command, it ommits any warning with this enum state.
        /// </summary>
        Revoked

    }

}

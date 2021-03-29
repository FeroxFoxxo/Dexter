namespace Dexter.Databases.Reminders {

    /// <summary>
    /// Gives information about the status of a reminder relative to the reminder release system.
    /// It can be PENDING, REMOVED, or RELEASED.
    /// </summary>

    public enum ReminderStatus {
        
        /// <summary>
        /// For reminders that are still pending release in the future.
        /// </summary>
        Pending,

        /// <summary>
        /// For reminders that have never been released and have been removed from the system.
        /// </summary>
        Removed,

        /// <summary>
        /// For reminders that have completed deployment.
        /// </summary>
        Released
    }

}

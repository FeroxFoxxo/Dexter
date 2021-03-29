namespace Dexter.Databases.CommunityEvents {

    /// <summary>
    /// Keeps track of what stage of approval an event submission is in. It can be PENDING, APPROVED, DENIED, or RELEASED.
    /// </summary>

    public enum EventStatus {

        /// <summary>
        /// An item that still has not gone through the admin approval process.
        /// </summary>

        Pending,

        /// <summary>
        /// An item that has successfully gone through the admin approval process and is pending release.
        /// </summary>

        Approved,

        /// <summary>
        /// An item that has failed to go through the admin approval process or has been removed.
        /// </summary>

        Denied,

        /// <summary>
        /// An item that has not been denied nor approved, and whose release time is overdue.
        /// </summary>

        Expired,

        /// <summary>
        /// An item that has been removed from the proposals system.
        /// </summary>

        Removed,

        /// <summary>
        /// An item that was approved by admins and has been released to the public.
        /// </summary>

        Released

    }
}

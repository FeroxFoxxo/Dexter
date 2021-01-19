namespace Dexter.Enums {

    /// <summary>
    /// The type an infraction falls into.
    /// An infraction may be of type WARNING, MUTE, or INDEFINITEMUTE.
    /// </summary>

    public enum InfractionType {

        /// <summary>
        /// Infraction which lacks a mute, and is supposed to serve as a heads-up to a user.
        /// </summary>

        Warning,

        /// <summary>
        /// Punitive infraction which renders the user unable to send messages for a set duration.
        /// </summary>

        Mute,

        /// <summary>
        /// Punitive infraction which renders the user unable to send messages indefinitely.
        /// </summary>

        IndefiniteMute

    }

}
 
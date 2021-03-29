using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Reminders {

    /// <summary>
    /// Represents a reminder item in the Dexter Reminder system.
    /// </summary>
    public class Reminder {

        /// <summary>
        /// The Unique identifier for the reminder within the database.
        /// </summary>

        [Key]
        public int ID { get; set; }

        /// <summary>
        /// The unique User ID of the user who set up the reminder.
        /// </summary>

        public ulong IssuerID { get; set; }

        /// <summary>
        /// The date and time to send the reminder at, in seconds since Unix Time.
        /// </summary>

        public long DateTimeRelease { get; set; }

        /// <summary>
        /// The message the user wants to be sent by the bot as a reminder.
        /// </summary>

        public string Message { get; set; }

        /// <summary>
        /// The status of the reminder relative to the events system.
        /// </summary>

        public ReminderStatus Status { get; set; }

    }
}

using Dexter.Configurations;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dexter.Databases.GreetFur
{

    /// <summary>
    /// A record for a specific GreetFur and a specific day
    /// </summary>

    public class GreetFurRecord
    {
        /// <summary>
        /// Unique identifier for the record object.
        /// </summary>
        [Key]
        public uint RecordId { get; set; }

        /// <summary>
        /// Unique user identifier for the user this record refers to.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// The amount of days since UNIX time before the day that this record represents.
        /// </summary>
        public int Date { get; set; }
        
        /// <summary>
        /// The amount of GreetFur-eligible messages sent by the user in the given day.
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Marks the kind of activity that this user has had for the given day.
        /// </summary>
        public ActivityFlags Activity { get; set; }

        /// <summary>
        /// Whether the user muted another user via the GreetFur-specific mute command in the selected day.
        /// </summary>
        [NotMapped]
        public bool MutedUser { 
            get
            {
                return Activity.HasFlag(ActivityFlags.MutedUser);
            }
            set
            {
                if (value)
                {
                    Activity |= ActivityFlags.MutedUser;
                } 
                else
                {
                    Activity &= ~ActivityFlags.MutedUser;
                }
            }
        }

        /// <summary>
        /// Whether the user muted another user via the GreetFur-specific mute command in the selected day.
        /// </summary>
        [NotMapped]
        public bool IsExempt {
            get
            {
                return Activity.HasFlag(ActivityFlags.Exempt);
            }
            set
            {
                if (value)
                {
                    Activity |= ActivityFlags.Exempt;
                }
                else
                {
                    Activity &= ~ActivityFlags.Exempt;
                }
            }
        }


        /// <summary>
        /// Converts the record into a human-readable format depicting the completion state of the record.
        /// </summary>
        /// <param name="greetFurConfiguration">The configuration file setting standards for completion</param>
        /// <param name="currentDay">The current day for the user.</param>
        /// <returns>A short string representation of the completion state of this record.</returns>

        public string ToString(GreetFurConfiguration greetFurConfiguration, int currentDay = int.MaxValue)
        {
            string state;
            if (IsYes(greetFurConfiguration))
                state = "Y";
            else if (IsExempt)
                return "Exempt";
            else if (Date == currentDay)
                state = "?";
            else if (Date < currentDay)
                state = "N";
            else
                return "";

            return string.Format("{0} ({1}{2})",
                state,
                MutedUser ? "M" : "",
                MessageCount);
        }

        /// <summary>
        /// Creates a string representation of the data held in the record.
        /// </summary>
        /// <returns>A string representing the values held in the record.</returns>

        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Creates a string representation of the data held in the record.
        /// </summary>
        /// <param name="shortened">Whether to use a short format.</param>
        /// <returns>A string representing the values held in the record.</returns>

        public string ToString(bool shortened)
        {
        if (shortened)
            return $"{UserId}-{Date}; {(MutedUser ? "M" : "")}{MessageCount}";
        else
            return $"{RecordId}: {UserId} day {Date}; {MessageCount} messages {(MutedUser ? "with" : "without")} mute.";
        }

        /// <summary>
        /// Checks whether the required activity quota has been achieved by the user in this record object.
        /// </summary>
        /// <param name="config">The GreetFur Configuration file containing relevant information defining whether a user's activity is considered as an active day or not.</param>
        /// <returns><see langword="true"/> if the user's activity matches a "Yes", otherwise <see langword="false"/>.</returns>

        public bool IsYes(GreetFurConfiguration config)
        {
            return MessageCount >= config.GreetFurMinimumDailyMessages || (MutedUser && config.GreetFurActiveWithMute);
        }
    }

    /// <summary>
    /// Marks the kind of activity that a GreetFur has completed for a given day.
    /// </summary>

    [Flags]
    public enum ActivityFlags
    {
        /// <summary>
        /// Marks standard activity
        /// </summary>
        None = 0,
        /// <summary>
        /// Marks that the user has muted another user during this record's time.
        /// </summary>
        MutedUser = 1,
        /// <summary>
        /// Marks that the user is exempt from participating during the record's time.
        /// </summary>
        Exempt = 2
    }
}

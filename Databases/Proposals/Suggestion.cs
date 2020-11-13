using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Proposals {

    /// <summary>
    /// The Suggestion class contains information on a suggested proposal, including an automatically
    /// generated alphanumeric tracker as its key, the message IDs for each respective embe and the expiry of the suggestion.
    /// </summary>
    public class Suggestion {

        /// <summary>
        /// The TrackerID field is the KEY of the table. It is unique per suggestion.
        /// It is an alphanumeric, 8 character long token that is randomly generated.
        /// </summary>
        [Key]
        public string Tracker { get; set; }

        /// <summary>
        /// The Staff Message ID is the snowflake ID of the embed that has been put forth in the #staff-suggestion channel.
        /// </summary>
        public ulong StaffMessageID { get; set; }

        /// <summary>
        /// The Expiry field is the UNIX time at which the suggestion will automatically decline.
        /// </summary>
        public string Expiry { get; set; }

    }

}

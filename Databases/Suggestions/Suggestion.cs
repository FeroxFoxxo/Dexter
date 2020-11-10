using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Suggestions {
    /// <summary>
    /// The Suggestion class contains information on a suggested proposal, including an automatically
    /// generated alphanumeric tracker as its key, the ID of the suggestor, the reason, content, status
    /// and the expiry of the suggestion. It also includes the message IDs for each respective embed.
    /// </summary>
    public class Suggestion {

        /// <summary>
        /// The TrackerID field is the KEY of the table. It is unique per suggestion.
        /// It is an alphanumeric, 8 character long token that is randomly generated.
        /// </summary>
        [Key]
        public string TrackerID { get; set; }

        /// <summary>
        /// The Suggestor field is the snowflake ID of the user who had suggested the respective proposal.
        /// </summary>
        public ulong Suggestor { get; set; }

        /// <summary>
        /// The Status field is the status of the suggestion that has been put fourth.
        /// This can be either SUGGESTED, PENDING, APPROVED, DENIED or DELETED.
        /// </summary>
        public SuggestionStatus Status { get; set; }

        /// <summary>
        /// The Content field is a string of the suggestion that has been proposed.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The Reason field specifies the reason a suggestion has been approved or denied.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// The Message ID field is the snowflake ID of the embed that has been put forth in the #suggestion channel.
        /// </summary>
        public ulong MessageID { get; set; }

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

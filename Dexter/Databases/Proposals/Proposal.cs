using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Proposals {

    /// <summary>
    /// The Proposal class contains information on a suggested proposal, including an automatically
    /// generated alphanumeric tracker as its key, the ID of the suggestor, the reason, content and status.
    /// </summary>
    
    public class Proposal {

        /// <summary>
        /// The TrackerID field is the KEY of the table. It is unique per suggestion.
        /// It is an alphanumeric, 8 character long token that is randomly generated.
        /// </summary>
        
        [Key]

        public string Tracker { get; set; }

        /// <summary>
        /// The Proposer field is the snowflake ID of the user who had suggested the respective proposal.
        /// </summary>
        
        public ulong Proposer { get; set; }

        /// <summary>
        /// The ProposalStatus field is the status of the suggestion that has been put fourth.
        /// This can be either SUGGESTED, PENDING, APPROVED, DENIED or DELETED.
        /// </summary>
        
        public ProposalStatus ProposalStatus { get; set; }

        /// <summary>
        /// The Proposal TopicType field is the type of proposal this is, whether that be an admin confirmation or a suggestion.
        /// </summary>
        
        public ProposalType ProposalType { get; set; }

        /// <summary>
        /// The Content field is a string of the suggestion that has been proposed.
        /// </summary>
        
        public string Content { get; set; }

        /// <summary>
        /// The Reason field specifies the reason a suggestion has been approved or denied.
        /// </summary>
        
        public string Reason { get; set; }

        /// <summary>
        /// The Message ID field is the snowflake ID of the embed that has been put forth in the #suggestions channel.
        /// </summary>
        
        public ulong MessageID { get; set; }

        /// <summary>
        /// The Proxy URL field is the URL of the attachment which has been fed through the storage channel.
        /// </summary>
        
        public string ProxyURL { get; set; }

        /// <summary>
        /// The true avatar URL of the user who first made the proposal.
        /// </summary>

        public string AvatarURL { get; set; }

        /// <summary>
        /// The username of the user who first made the proposal.
        /// </summary>

        public string Username { get; set; }
    }

}

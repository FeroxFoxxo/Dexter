using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.CommunityEvents {

    /// <summary>
    /// An event proposed by a member of the community.
    /// </summary>

    public class CommunityEvent {

        /// <summary>
        /// The unique numeric ID for a given event and the token by which to target events.
        /// </summary>

        [Key]

        public int ID { get; set; }

        /// <summary>
        /// The unique user ID of the user who proposed the event.
        /// </summary>

        public ulong ProposerID { get; set; }

        /// <summary>
        /// The time and date when the event was first proposed and added to the database.
        /// </summary>

        public long DateTimeProposed { get; set; }

        /// <summary>
        /// The time and date when the event is to be released to the public, allows for scheduling events.
        /// </summary>

        public long DateTimeRelease { get; set; }

        /// <summary>
        /// The alphanumerical token for the Timer associated to the release of this event.
        /// </summary>

        public string ReleaseTimer { get; set; }

        /// <summary>
        /// The long description of an event that describes and gives all relevant links and information as to what the event is about.
        /// </summary>

        public string Description { get; set; }

        /// <summary>
        /// The status of the event in regards to the approval and release process.
        /// </summary>

        public EventStatus Status { get; set; }

        /// <summary>
        /// The reason behind the resolution of an event proposal.
        /// </summary>

        public string ResolveReason { get; set; }

        /// <summary>
        /// The message ID for the proposal notification attached to this event.
        /// </summary>

        public ulong EventProposal { get; set; }

        /// <summary>
        /// Whether the event is user-hosted or an official event.
        /// </summary>

        public EventType EventType { get; set; }

    }
}

using Dexter.Abstractions;
using Dexter.Databases.CommunityEvents;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// Holds settings and server-specific data related to the managing of community resources such as events.
    /// </summary>

    public class CommunityConfiguration : JSONConfig {

        /// <summary>
        /// The unique id of the channel where event proposals are to be sent.
        /// </summary>

        public ulong EventsNotificationsChannel { get; set; }

        /// <summary>
        /// The string of mention(s) to be attached to each new event proposal.
        /// </summary>

        public string EventsNotificationMention { get; set; }

        /// <summary>
        /// The ID of the "Community Events Notified" role, used for User-Hosted events.
        /// </summary>

        public ulong CommunityEventsNotifiedRole { get; set; }

        /// <summary>
        /// The unique channel ID for the #community-events message channel.
        /// </summary>

        public ulong CommunityEventsChannel { get; set; }

        /// <summary>
        /// The ID of the "Events Notified" role, used for Official server events.
        /// </summary>

        public ulong OfficialEventsNotifiedRole { get; set; }

        /// <summary>
        /// The unique channel ID for the #official-server-events message channel.
        /// </summary>

        public ulong OfficialEventsChannel { get; set; }

        /// <summary>
        /// If <see langword="true"/>, an event approved after its release date will send a DM to the host saying it failed instead of releasing the event. If <see langword="false"/>, it releases the event immediately.
        /// </summary>

        public bool FailOnOverdueApproval { get; set; }

        /// <summary>
        /// The maximum amount of events to be shown on the page of an embedmenu for the <c>~event get user [USER]</c> command option.
        /// </summary>

        public short MaxEventsPerMenu { get; set; }

        /// <summary>
        /// Sets a different embed color for a given event status. The values are 24-bit colors formatted in hexadecimal and stringified.
        /// </summary>

        public Dictionary<EventStatus, string> EventStatusColor { get; set;}

        /// <summary>
        /// Sets whether command help for approving or declining an event should be included in event proposals.
        /// </summary>

        public bool IncludeEventResolutionInfo { get; set; }
    }
}

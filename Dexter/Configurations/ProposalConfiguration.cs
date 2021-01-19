using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// The ProposalConfiguration relates to all configuration data that the suggestion service requires to operate.
    /// </summary>
    
    public class ProposalConfiguration : JSONConfig {
        
        /// <summary>
        /// The SUGGESTION CHANNEL specifies the snowflake ID of the channel in which proposals are to be sent in.
        /// </summary>
        
        public ulong SuggestionsChannel { get; set; }

        /// <summary>
        /// The STAFF SUGGESTIONS CHANNEL specifies the snowflake ID of the channel in which the pending proposals are to be sent in.
        /// </summary>
        
        public ulong StaffSuggestionsChannel { get; set; }

        /// <summary>
        /// The STORAGE GUILD specifies the snowflake ID of the server that the suggestion emoji are located in and where images will be stored.
        /// </summary>
        
        public ulong StorageGuild { get; set; }

        /// <summary>
        /// The EMOJI field is a dictionary of the names of the suggestion emoji, corresponding with their IDs.
        /// </summary>
        
        public Dictionary<string, ulong> Emoji { get; set; }

        /// <summary>
        /// The SUGGESTION EMOJI specifies the emoji that should be applied to a proposed suggestion in the #suggestions channel.
        /// </summary>
        
        public List<string> SuggestionEmoji { get; set; }

        /// <summary>
        /// The STAFF SUGGESTION EMOJI specifies the emoji that should be applied to a proposal up for staff voting.
        /// </summary>
        
        public List<string> StaffSuggestionEmoji { get; set; }

        /// <summary>
        /// The REACTION PASS field specifies the amount of reactions that are required to pass the suggested content over to the staff
        /// voting stage or to deny it from the community voting stage.
        /// </summary>
        
        public int ReactionPass { get; set; }

        /// <summary>
        /// The STORAGE CHANNEL ID specifies the snowflake ID of the channel Dexter will store images of messages sent.
        /// </summary>

        public ulong StorageChannelID { get; set; }

        /// <summary>
        /// A list of commands which won't throw an exception of type "unknown command", but will also do nothing.
        /// </summary>

        public List<string> CommandRemovals { get; set; }

        /// <summary>
        /// The maximum amount of time a suggestion is allowed to remain active before being automatically declined. 
        /// </summary>

        public int IdleDeclineTime { get; set; }

        /// <summary>
        /// The absolute value of the difference in votes for a suggestion in the staff approval stage to be automatically accepted or declined.
        /// </summary>

        public int StaffVotingThreshold { get; set; }

    }

}

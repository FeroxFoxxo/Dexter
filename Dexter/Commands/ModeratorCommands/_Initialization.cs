using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Cooldowns;
using Dexter.Databases.Relay;
using Dexter.Databases.Infractions;
using Discord.WebSocket;

namespace Dexter.Commands {

    /// <summary>
    /// The ModeratorCommands module relates to recording of users in breach of rules and other, miscelaneous commands relating to the moderation of the server.
    /// </summary>

    public partial class ModeratorCommands : DiscordModule {

        /// <summary>
        /// The CooldownDB stores the cooldowns that the cooldown command interfaces with.
        /// </summary>
        
        public CooldownDB CooldownDB { get; set; }

        /// <summary>
        /// The InfractionsDB stores the warnings that these commands interface.
        /// </summary>
        
        public InfractionsDB InfractionsDB { get; set; }

        /// <summary>
        /// The RelayDB stores the relays that are used to send messages to a channel in set intervals.
        /// </summary>
        
        public RelayDB RelayDB { get; set; }

        /// <summary>
        /// The Client is an instance of the DiscordSocketClient, used to get a user on callback.
        /// </summary>
        
        public DiscordSocketClient Client { get; set; }

        /// <summary>
        /// The CommissionCooldownConfiguration stores the length of time a commission cooldown lasts for.
        /// </summary>
        
        public CommissionCooldownConfiguration CommissionCooldownConfiguration { get; set; }

        public ModerationConfiguration ModerationConfiguration { get; set; }

    }

}

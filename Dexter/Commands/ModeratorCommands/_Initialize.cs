using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Cooldowns;
using Dexter.Databases.Relay;
using Dexter.Databases.Warnings;
using Discord.WebSocket;

namespace Dexter.Commands {

    /// <summary>
    /// The ModeratorCommands module relates to recording of users in breach of rules and other, miscelaneous commands relating to the moderation of the server.
    /// </summary>
    public partial class ModeratorCommands : DiscordModule {

        private readonly CooldownDB CooldownDB;

        private readonly WarningsDB WarningsDB;

        private readonly RelayDB RelayDB;

        private readonly DiscordSocketClient Client;

        private readonly CommissionCooldownConfiguration CommissionCooldownConfiguration;

        /// <summary>
        /// The constructor for the ModeratorCommands module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="WarningsDB">The WarningsDB stores the warnings that these commands interface.</param>
        /// <param name="CooldownDB">The CooldownDB stores the cooldowns that the cooldown command interfaces with.</param>
        /// <param name="RelayDB">The RelayDB stores the relays that are used to send messages to a channel in set intervals.</param>
        /// <param name="CommissionCooldownConfiguration">The CommissionCooldownConfiguration stores the length of time a commission cooldown lasts for.</param>
        /// <param name="Client">The Client is an instance of the DiscordSocketClient, used to get a user on callback.</param>
        public ModeratorCommands(WarningsDB WarningsDB, CooldownDB CooldownDB, RelayDB RelayDB,
                CommissionCooldownConfiguration CommissionCooldownConfiguration, DiscordSocketClient Client) {
            this.WarningsDB = WarningsDB;
            this.CooldownDB = CooldownDB;
            this.RelayDB = RelayDB;
            this.Client = Client;
            this.CommissionCooldownConfiguration = CommissionCooldownConfiguration;
        }

    }
}

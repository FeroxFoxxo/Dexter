using Dexter.Abstractions;
using Dexter.Databases.Warnings;
using Discord.WebSocket;

namespace Dexter.Commands {

    /// <summary>
    /// The WarningCommands module relates to the warning and recording of a user on breach of the rules.
    /// </summary>
    public partial class WarningCommands : DiscordModule {

        private readonly WarningsDB WarningsDB;

        private readonly DiscordSocketClient Client;

        /// <summary>
        /// The constructor for the ProposalCommands module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="WarningsDB">The WarningsDB stores the warnings that these commands interface.</param>
        /// <param name="Client">The Client is an instance of the DiscordSocketClient, used to get a user on callback.</param>
        public WarningCommands(WarningsDB WarningsDB, DiscordSocketClient Client) {
            this.WarningsDB = WarningsDB;
            this.Client = Client;
        }

    }
}

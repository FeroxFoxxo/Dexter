using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;
using Dexter.Databases.FunTopics;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands within the Fun module.
    /// </summary>

    public partial class FunCommands : DiscordModule {

        /// <summary>
        /// Works as an interface between the configuration files attached to the Fun module and the commands.
        /// </summary>

        public FunConfiguration FunConfiguration { get; set; }

        /// <summary>
        /// Loads the database containing topics for the <code>~topic</code> command.
        /// </summary>

        public FunTopicsDB FunTopicsDB { get; set; }

    }

}

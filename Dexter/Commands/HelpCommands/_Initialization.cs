using Dexter.Abstractions;
using Dexter.Attributes.Classes;
using Discord.Commands;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands within the Help module.
    /// </summary>

    [EssentialModule]
    public partial class HelpCommands : DiscordModule {

        /// <summary>
        /// Service responsible for parsing and overall managing interaction with commands issued by users.
        /// </summary>

        public CommandService CommandService { get; set; }

    }

}

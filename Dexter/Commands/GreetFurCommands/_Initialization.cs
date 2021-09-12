using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.GreetFur;
using Dexter.Services;
using Google.Apis.Sheets.v4;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands within the GreetFur module.
    /// </summary>

    public partial class GreetFurCommands : DiscordModule
    {

        /// <summary>
        /// Allows logging necessary data pertaining to issues or important information during interaction with the command environment.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// Works as an interface between the configuration files attached to the GreetFur module and the commands.
        /// </summary>

        public GreetFurConfiguration GreetFurConfiguration { get; set; }

        /// <summary>
        /// A wrapper for useful GreetFur data and a wrapper for spreadsheet manipulation and access.
        /// </summary>

        public GreetFurService GreetFurService { get; set; }

        /// <summary>
        /// A database with information about GreetFur activity records.
        /// </summary>

        public GreetFurDB GreetFurDB { get; set; }

        /// <summary>
        /// A reference to the command module responsible for muting users.
        /// </summary>

        public ModeratorCommands ModeratorCommands { get; set; }

    }

}

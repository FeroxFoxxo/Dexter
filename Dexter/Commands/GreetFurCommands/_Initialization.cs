using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Services;
using Google.Apis.Sheets.v4;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands within the GreetFur module.
    /// </summary>

    public partial class GreetFurCommands : DiscordModule {

        /// <summary>
        /// Allows logging necessary data pertaining to issues or important information during interaction with the command environment.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// Works as an interface between the configuration files attached to the GreetFur module and the commands.
        /// </summary>

        public GreetFurConfiguration GreetFurConfiguration { get; set; }

        /// <summary>
        /// The sheets service allows the bot to interface with the GreetFur spreadsheets.
        /// </summary>

        public SheetsService SheetsService;

    }

}

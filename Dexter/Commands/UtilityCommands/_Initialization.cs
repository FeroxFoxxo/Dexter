using Dexter.Abstractions;
using Dexter.Services;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands within the Utility module.
    /// </summary>

    public partial class UtilityCommands : DiscordModule {

        /// <summary>
        /// Allows logging necessary data pertaining to issues or important information during interaction with the command environment.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// Coordinates the initialization of all necessary infrastructure upon startup.
        /// </summary>

        public StartupService StartupService { get; set; }

    }

}

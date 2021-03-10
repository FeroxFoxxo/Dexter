using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Borkdays;
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
        /// The BorkdayDB stores information regarding a user's birthday.
        /// </summary>

        public BorkdayDB BorkdayDB { get; set; }

        /// <summary>
        /// Coordinates the initialization of all necessary infrastructure upon startup.
        /// </summary>

        public StartupService StartupService { get; set; }

        /// <summary>
        /// Contains information relative to organic language management and time zones.
        /// </summary>

        public LanguageConfiguration LanguageConfiguration { get; set; }

    }

}

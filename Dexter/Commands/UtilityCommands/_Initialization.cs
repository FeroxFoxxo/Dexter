using Dexter.Abstractions;
using Dexter.Services;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands within the Utility module.
    /// </summary>

    public partial class UtilityCommands : DiscordModule {

        public LoggingService LoggingService { get; set; }

        public StartupService StartupService { get; set; }

    }

}

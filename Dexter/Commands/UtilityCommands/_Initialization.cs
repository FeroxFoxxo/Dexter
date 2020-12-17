using Dexter.Abstractions;
using Dexter.Services;

namespace Dexter.Commands {

    public partial class UtilityCommands : DiscordModule {

        public LoggingService LoggingService { get; set; }

        public StartupService StartupService { get; set; }

    }

}

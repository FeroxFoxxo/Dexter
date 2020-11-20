using Dexter.Abstractions;
using Dexter.Services;

namespace Dexter.Commands {
    public partial class UtilityCommands : DiscordModule {

        private readonly LoggingService LoggingService;

        public UtilityCommands(LoggingService LoggingService) {
            this.LoggingService = LoggingService;
        }

    }
}

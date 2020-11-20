using Dexter.Abstractions;
using Dexter.Services;

namespace Dexter.Commands {
    public partial class UtilityCommands : DiscordModule {

        private readonly LoggingService LoggingService;

        private readonly ProfileService ProfileService;

        public UtilityCommands(LoggingService LoggingService, ProfileService ProfileService) {
            this.LoggingService = LoggingService;
            this.ProfileService = ProfileService;
        }

    }
}

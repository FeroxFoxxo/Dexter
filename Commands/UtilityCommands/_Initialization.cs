using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Services;

namespace Dexter.Commands {
    public partial class UtilityCommands : DiscordModule {

        private readonly LoggingService LoggingService;

        private readonly BotConfiguration BotConfiguration;

        public UtilityCommands(BotConfiguration _BotConfiguration, LoggingService _LoggingService) : base (_BotConfiguration) {
            BotConfiguration = _BotConfiguration;
            LoggingService = _LoggingService;
        }

    }
}

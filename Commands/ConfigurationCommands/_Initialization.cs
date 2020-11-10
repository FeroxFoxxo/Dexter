using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Services;

namespace Dexter.Commands {
    [EssentialModule]
    public partial class ConfigurationCommands : DiscordModule {

        private readonly ModuleService ModuleService;
        private readonly BotConfiguration BotConfiguration;

        public ConfigurationCommands(ModuleService _ModuleService, BotConfiguration _BotConfiguration) {
            ModuleService = _ModuleService;
            BotConfiguration = _BotConfiguration;
        }

    }
}

using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Core.Attributes;
using Dexter.Services;

namespace Dexter.Commands {
    [EssentialModule]
    public partial class ConfigurationCommands : ModuleD {

        private readonly ModuleService ModuleService;
        private readonly BotConfiguration BotConfiguration;

        public ConfigurationCommands(ModuleService _ModuleService, BotConfiguration _BotConfiguration) {
            ModuleService = _ModuleService;
            BotConfiguration = _BotConfiguration;
        }

    }
}

using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Configuration;
using Dexter.Services;
using Discord.Commands;

namespace Dexter.Commands.ConfigurationCommands {
    [EssentialModule]
    public partial class ConfigurationCommands : Module {

        private readonly ModuleService ModuleService;
        private readonly BotConfiguration BotConfiguration;

        public ConfigurationCommands(ModuleService _ModuleService, BotConfiguration _BotConfiguration) {
            ModuleService = _ModuleService;
            BotConfiguration = _BotConfiguration;
        }

    }
}

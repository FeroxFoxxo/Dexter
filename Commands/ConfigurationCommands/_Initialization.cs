using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Services;

namespace Dexter.Commands {

    /// <summary>
    /// The ConfigurationCommands module relates to the enabling and disabling of a command module.
    /// It is an EssentialModule and, as such, it will not be able to be disabled. 
    /// </summary>
    
    [EssentialModule]

    public partial class ConfigurationCommands : DiscordModule {

        private readonly ModuleService ModuleService;
        private readonly BotConfiguration BotConfiguration;

        /// <summary>
        /// The constructor for the ConfigurationCommands module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="ModuleService">The ModuleService is what we use to enable and disable the modules, linked to the CommandService.</param>
        /// <param name="BotConfiguration">The BotConfiguration is used to get the prefix of commands.</param>
        public ConfigurationCommands(ModuleService ModuleService, BotConfiguration BotConfiguration) {
            this.ModuleService = ModuleService;
            this.BotConfiguration = BotConfiguration;
        }

    }

}

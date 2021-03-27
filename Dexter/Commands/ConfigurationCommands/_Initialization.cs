using Dexter.Abstractions;
using Dexter.Attributes.Classes;
using Dexter.Services;

namespace Dexter.Commands {

    /// <summary>
    /// The ConfigurationCommands module relates to the enabling and disabling of a command module.
    /// It is an EssentialModule and, as such, it will not be able to be disabled. 
    /// </summary>

    [EssentialModule]

    public partial class ConfigurationCommands : DiscordModule {

        /// <summary>
        /// The ModuleService is what we use to enable and disable the modules, linked to the CommandService.
        /// </summary>
        
        public ModuleService ModuleService { get; set; }

    }

}

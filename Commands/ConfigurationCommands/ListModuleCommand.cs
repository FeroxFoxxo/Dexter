using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Configuration;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class ConfigurationCommands {

        [Command("modules")]
        [Summary("Lists all modules.")]
        [RequireAdministrator]

        public async Task ListModulesAsync()
            => await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Modules")
                .WithDescription($"Use `{BotConfiguration.Prefix}module enable/disable <name>` to change the state of a module!")
                .WithFields(
                    CreateModuleListField("Enabled", ModuleService.GetModules(ConfigurationType.Enabled)),
                    CreateModuleListField("Disabled", ModuleService.GetModules(ConfigurationType.Disabled)),
                    CreateModuleListField("Essential", ModuleService.GetModules(ConfigurationType.Essential))
                )
                .SendEmbed(Context.Channel);

        private static EmbedFieldBuilder CreateModuleListField(string Name, string[] Modules)
            => new EmbedFieldBuilder()
                .WithName(Name)
                .WithValue(Modules.Length > 0 ? string.Join("\n", Modules) : "No Modules")
                .WithIsInline(true);

    }
}

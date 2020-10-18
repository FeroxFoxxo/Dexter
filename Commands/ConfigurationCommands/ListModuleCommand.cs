using Dexter.Core.Attributes;
using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Dexter.Databases.Configuration;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.ConfigurationCommands {
    public partial class ConfigurationCommands {

        [Command("modules")]
        [Summary("Lists all modules.")]
        [RequireAdministrator]

        public async Task ListModulesAsync()
            => await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Modules")
                .WithDescription($"Use `{BotConfiguration.Prefix}module enable/disable <name>` to change the state of a module!")
                .WithFields(
                    CreateModuleListField("Enabled", ModuleService.GetModules(ConfigrationType.Enabled)),
                    CreateModuleListField("Disabled", ModuleService.GetModules(ConfigrationType.Disabled)),
                    CreateModuleListField("Essential", ModuleService.GetModules(ConfigrationType.Essential))
                )
                .SendEmbed(Context.Channel);

        private static EmbedFieldBuilder CreateModuleListField(string Name, string[] Modules)
            => new EmbedFieldBuilder()
                .WithName(Name)
                .WithValue(Modules.Length > 0 ? string.Join("\n", Modules) : "No Modules")
                .WithIsInline(true);

    }
}

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
        [Alias("module", "mod")]

        public async Task ListModulesAsync()
            => await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Modules:")
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

        [Command("modules")]
        [Summary("Performs an action on a module.")]
        [RequireAdministrator]
        [Alias("module", "mod")]

        public async Task ModifyModuleAsync(ModuleActionType ModuleActionType, string ModuleName) {
            if (Services.ModuleService.VerifyModuleName(ref ModuleName)) {
                bool IsActive = ModuleService.GetModuleState(ModuleName);

                switch (ModuleActionType) {
                    case ModuleActionType.Status:
                        await BuildEmbed(IsActive ? EmojiEnum.Love : EmojiEnum.Annoyed)
                            .WithTitle("Module ProposalStatus.")
                            .WithDescription($"The module **{ModuleName}** is currently **{(IsActive ? "enabled" : "disabled")}.**")
                            .SendEmbed(Context.Channel);
                        break;
                    default:
                        if ((ModuleActionType == ModuleActionType.Enable && IsActive) || (ModuleActionType == ModuleActionType.Disable && !IsActive))
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Module already set to value!")
                                .WithDescription($"The module **{ModuleName}** is already **{(IsActive ? "enabled" : "disabled")}.**")
                                .SendEmbed(Context.Channel);
                        else {
                            bool Active = ModuleActionType == ModuleActionType.Enable;
                            await ModuleService.SetModuleState(ModuleName, Active);
                            await BuildEmbed(Active ? EmojiEnum.Love : EmojiEnum.Annoyed)
                                .WithTitle("Module set!")
                                .WithDescription($"The module **{ModuleName}** is now **{(Active ? "enabled" : "disabled")}.**")
                                .SendEmbed(Context.Channel);
                        }
                        break;
                }
            } else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Module!")
                    .WithDescription($"I don't know a module called **{ModuleName}.**")
                    .SendEmbed(Context.Channel);
        }
    }
}

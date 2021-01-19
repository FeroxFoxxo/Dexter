using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Configurations;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ConfigurationCommands {

        /// <summary>
        /// The ListModule method runs on MODULES and will list all the enabled, disabled and essential commands when
        /// given no parameters to the command, sending it into the specified channel.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("modules")]
        [Summary("Lists all modules in the bot's arsenal.")]
        [RequireAdministrator]
        [Alias("module", "mod")]

        public async Task ListModules()
            => await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Modules:")
                .WithDescription($"Use `{BotConfiguration.Prefix}module enable/disable <name>` to change the state of a module!")
                .AddField("Enabled", ModuleService.GetModules(ConfigurationType.Enabled), true)
                .AddField("Disabled", ModuleService.GetModules(ConfigurationType.Disabled), true)
                .AddField("Essential", ModuleService.GetModules(ConfigurationType.Essential), true)
                .SendEmbed(Context.Channel);

        /// <summary>
        /// The ModifyModule method runs on MODULES and will set a given module to a specific module type or will
        /// reply with the status of the given module to the channel that the command had been send to.
        /// </summary>
        /// <param name="ModuleActionType">The ModuleActionType specifies the action you wish to apply to the command, whether that be ENABLE, DISABLE of LIST.</param>
        /// <param name="ModuleName">The ModuleName specifies the name of the module that you wish the action to be applied to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("modules")]
        [Summary("Performs an action on a module in the command service.")]
        [RequireAdministrator]
        [Alias("module", "mod")]

        public async Task ModifyModule(ModuleActionType ModuleActionType, string ModuleName) {
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

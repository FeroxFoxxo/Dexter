using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class ConfigurationCommands {

        [Command("module")]
        [Summary("Performs an action on a module.")]
        [RequireAdministrator]
        [Alias("mod")]

        public async Task ModifyModuleAsync(ModuleActionType ModuleAction, string ModuleName) {
            if (Services.ModuleService.VerifyModuleName(ref ModuleName)) {
                bool IsActive = ModuleService.GetModuleState(ModuleName);

                switch (ModuleAction) {
                    case ModuleActionType.Status:
                        await BuildEmbed(IsActive ? EmojiEnum.Love : EmojiEnum.Annoyed)
                            .WithTitle("Module Status")
                            .WithDescription($"The module **{ModuleName}** is currently **{(IsActive ? "enabled" : "disabled")}**.")
                            .SendEmbed(Context.Channel);
                        break;
                    default:
                        if ((ModuleAction == ModuleActionType.Enable && IsActive) || (ModuleAction == ModuleActionType.Disable && !IsActive))
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Module already set to value!")
                                .WithDescription($"The module **{ModuleName}** is already **{(IsActive ? "enabled" : "disabled")}**!")
                                .SendEmbed(Context.Channel);
                        else {
                            bool Active = ModuleAction == ModuleActionType.Enable;
                            await ModuleService.SetModuleState(ModuleName, Active);
                            await BuildEmbed(Active ? EmojiEnum.Love : EmojiEnum.Annoyed)
                                .WithTitle("Module set!")
                                .WithDescription($"The module **{ModuleName}** is now **{(Active ? "enabled" : "disabled")}**!")
                                .SendEmbed(Context.Channel);
                        }
                        break;
                }
            } else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Module!")
                    .WithDescription($"I don't know a module called **{ModuleName}**.")
                    .SendEmbed(Context.Channel);
        }

    }
}

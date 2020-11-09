using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class CustomCommands {

        [Command("ccremove")]
        [Summary("Removes a custom command.")]
        [Alias("ccdel", "ccdelete")]
        [RequireModerator]

        public async Task RemoveCommandAsync(string CommandName) {
            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            if (CustomCommandDB.GetCommandByNameOrAlias(CommandName) == null)
                throw new InvalidOperationException($"A command with the name `{CommandName}` doesn't exist!");

            CustomCommandDB.CustomCommands.Remove(Command);

            await CustomCommandDB.SaveChangesAsync();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The command `{CommandName}` was removed!")
                .WithDescription("If this was a mistake, please use `ccadd` to remake this command.\nWe hope you enjoyed using this command <3")
                .SendEmbed(Context.Channel);
        }

    }
}

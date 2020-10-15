using Dexter.Abstractions;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands.CustomCommands {
    public partial class CustomCommands {

        [Command("ccedit")]
        [Summary("Edit a list of custom commands.")]
        [Alias("ccchange")]

        public async Task EditCommandsAsync(string CommandName, [Remainder] string EditedReply) {
            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            if (CustomCommandDB.GetCommandByNameOrAlias(CommandName) == null)
                throw new InvalidOperationException($"A command with the name `{CommandName}` doesn't exist!");

            string OldReply = Command.Reply;

            Command.Reply = EditedReply;

            await CustomCommandDB.SaveChangesAsync();

            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The command `{CommandName}`'s reply was edited!")
                .WithDescription($"Changed command `{BotConfiguration.Prefix}{CommandName}` from `{OldReply}` to `{EditedReply}`")
                .SendEmbed(Context.Channel);
        }

    }
}
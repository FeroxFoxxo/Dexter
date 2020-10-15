using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Configuration;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands.CustomCommands {
    public partial class CustomCommands {

        [Command("ccadd")]
        [Summary("Creates a new customizeable command.")]
        [Alias("cccreate", "ccmake")]
        [RequireModerator]

        public async Task CreateCommandAsync(string CommandName, [Remainder] string Reply) {
            if (CustomCommandDB.GetCommandByNameOrAlias(CommandName) != null)
                throw new InvalidOperationException($"A command with the name `{CommandName}` already exists!");

            CustomCommandDB.CustomCommands.Add(new CustomCommand() {
                CommandName = CommandName.ToLower(),
                Reply = Reply,
                Alias = ""
            });

            await CustomCommandDB.SaveChangesAsync();

            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The command `{CommandName}` was added!")
                .WithDescription($"Use `{BotConfiguration.Prefix}ccalias add` to add an alias to the command! \n" +
                    "Please note, to make the command ping a user if mentioned, add `USER` to the reply~! \n" +
                    $"To modify the reply at any time, use ``{BotConfiguration.Prefix}ccedit``")
                .SendEmbed(Context.Channel);
        }

    }
}

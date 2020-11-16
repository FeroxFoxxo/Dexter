using Dexter.Configurations;
using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Dexter.Commands {
    public partial class CustomCommands {

        [Command("ccadd")]
        [Summary("Creates a new customizeable command.")]
        [Alias("cccreate", "ccmake")]
        [RequireModerator]

        public async Task CreateCommandAsync(string CommandName, [Remainder] string Reply) {
            if (CustomCommandDB.GetCommandByNameOrAlias(CommandName) != null)
                throw new InvalidOperationException($"A command with the name `{CommandName}` already exists!");

            if (Reply.Length > 1000)
                throw new InvalidOperationException($"Heya! Please cut down on your length of reply. " +
                    $"It should be a maximum of 1000 characters. Currently this character count sits at {Reply.Length}");

            await SendForAdminApproval(CreateCommandCallback,
                new Dictionary<string, string>() {
                    { "CommandName", CommandName.ToLower() },
                    { "Reply", Reply }
                },
                Context.Message.Author.Id,
                $"{Context.Message.Author.GetUserInformation()} has suggested that the command {CommandName} should be " +
                $"added with the reply of {Reply}.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The command `{CommandName}` was suggested!")
                .WithDescription($"Once it has passed admin approval," +
                    $"use `{BotConfiguration.Prefix}ccalias add` to add an alias to the command! \n" +
                    "Please note, to make the command ping a user if mentioned, add `USER` to the reply~! \n" +
                    $"To modify the reply at any time, use ``{BotConfiguration.Prefix}ccedit``")
                .SendEmbed(Context.Channel);
        }

        public async Task CreateCommandCallback(Dictionary<string, string> Parameters) {
            CustomCommandDB.CustomCommands.Add(new CustomCommand() {
                CommandName = Parameters["CommandName"],
                Reply = Parameters["Alias"],
                Alias = ""
            });

            await CustomCommandDB.SaveChangesAsync();
        }

    }
}

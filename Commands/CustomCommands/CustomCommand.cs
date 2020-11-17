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

        [Command("cc")]
        [Summary("Applies an action to a customizeable command.")]
        [Alias("customcommand", "command")]
        [RequireModerator]

        public async Task CustomCommand (ActionType ActionType, string CommandName, [Remainder] string Reply) {
            switch (ActionType) {
                case ActionType.Add:
                    if (CustomCommandDB.GetCommandByNameOrAlias(CommandName) != null)
                        throw new InvalidOperationException($"A command with the name `{BotConfiguration.Prefix}{CommandName}` already exists!");

                    if (Reply.Length > 1000)
                        throw new InvalidOperationException($"Heya! Please cut down on your length of reply. " +
                            $"It should be a maximum of 1000 characters. Currently this character count sits at {Reply.Length}");

                    await SendForAdminApproval(CreateCommandCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Reply", Reply }
                        },
                        Context.Message.Author.Id,
                        $"{Context.Message.Author.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should be " +
                        $"added with the reply of {Reply}.");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{CommandName}` was suggested!")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"use `{BotConfiguration.Prefix}ccalias add` to add an alias to the command! \n" +
                            "Please note, to make the command ping a user if mentioned, add `USER` to the reply~! \n" +
                            $"To modify the reply at any time, use `{BotConfiguration.Prefix}ccedit`")
                        .SendEmbed(Context.Channel);

                    break;
                case ActionType.Edit:
                    CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (Command == null)
                        throw new InvalidOperationException($"A command with the name `{BotConfiguration.Prefix}{CommandName}` doesn't exist!");

                    if (Reply.Length > 1000)
                        throw new InvalidOperationException($"Heya! Please cut down on your length of reply. " +
                            $"It should be a maximum of 1000 characters. Currently this character count sits at {Reply.Length}");

                    await SendForAdminApproval(EditCommandCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Reply", Reply }
                        },
                        Context.Message.Author.Id,
                        $"{Context.Message.Author.GetUserInformation()} has suggested that the command {BotConfiguration.Prefix}{CommandName} should be " +
                        $"edited from from `{Command.Reply}` to `{Reply}`");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The edit to `{BotConfiguration.Prefix}{CommandName}` was suggested!")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"The command `{BotConfiguration.Prefix}{CommandName}` will be changed to have the reply of {Reply} rahter than {Command.Reply}.")
                        .SendEmbed(Context.Channel);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(ActionType.ToString());
            }
        }


        [Command("cc")]
        [Summary("Applies an action to a customizeable command.")]
        [Alias("customcommand", "command")]
        [RequireModerator]

        public async Task CustomCommand(ActionType ActionType, string CommandName) {
            switch (ActionType) {
                case ActionType.Remove:
                    CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (Command == null)
                        throw new InvalidOperationException($"A command with the name `{BotConfiguration.Prefix}{CommandName}` doesn't exist!");

                    await SendForAdminApproval(RemoveCommandCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                        },
                        Context.Message.Author.Id,
                        $"{Context.Message.Author.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should be removed!"
                    );

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The removal of `{BotConfiguration.Prefix}{CommandName}` was suggested.")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"The command `{BotConfiguration.Prefix}{CommandName}` will be removed from the database.")
                        .SendEmbed(Context.Channel);
                    break;
            }
        }

        public async Task CreateCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Reply = Parameters["Reply"];

            CustomCommandDB.CustomCommands.Add(new CustomCommand() {
                CommandName = CommandName,
                Reply = Reply,
                Alias = ""
            });

            await CustomCommandDB.SaveChangesAsync();
        }
        
        public async Task EditCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Reply = Parameters["Reply"];

            CustomCommandDB.GetCommandByNameOrAlias(CommandName).Reply = Reply;

            await CustomCommandDB.SaveChangesAsync();
        }

        public async Task RemoveCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];

            CustomCommandDB.CustomCommands.Remove(CustomCommandDB.GetCommandByNameOrAlias(CommandName));

            await CustomCommandDB.SaveChangesAsync();
        }

    }

}

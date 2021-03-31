using Dexter.Configurations;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace Dexter.Commands {

    public partial class CustomCommands {

        /// <summary>
        /// The CustomCommand method runs on CC and will add/edit/remove/get a custom command in the CustomCommandDB
        /// based on the given ActionType and apply the REPLY parameter to it.
        /// </summary>
        /// <param name="ActionType">The ActionType specifies whether the action is to add or edit the command in the database.</param>
        /// <param name="CommandName">The CommandName specifies the command that you wish to add or edit from the database.</param>
        /// <param name="Reply">The Reply specifies the reply that you wish to given command to have.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("cc")]
        [Summary("Modifies a given customizeable command.\n" +
            "`ADD [COMMAND NAME] [REPLY]` - adds a custom command to the database with a given reply.\n" +
            "`EDIT [COMMAND NAME] [EDITED REPLY]` - edits a custom command's reply with a new one given.\n" +
            "`REMOVE [COMMAND NAME]` - removes a custom command from the database.\n" +
            "`GET [COMMAND NAME]` - gets information on a custom command, including its aliases and reply."
        )]
        [Alias("customcommand", "command")]
        [RequireModerator]

        public async Task CustomCommand (ActionType ActionType, string CommandName, [Optional] [Remainder] string Reply) {
            switch (ActionType) {
                case ActionType.Add:
                    if (string.IsNullOrEmpty(Reply)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Add Reply.")
                            .WithDescription("Reply is not given! Please enter a reply with this command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (CustomCommandDB.GetCommandByNameOrAlias(CommandName) != null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Add Reply.")
                            .WithDescription($"A command with the name `{BotConfiguration.Prefix}{CommandName}` already exists!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (Reply.Length > 1000) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Add Reply.")
                            .WithDescription("Heya! Please cut down on your length of reply. " +
                            $"It should be a maximum of 1000 characters. Currently this character count sits at {Reply.Length}!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await SendForAdminApproval(CreateCommandCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Reply", Reply }
                        },
                        Context.User.Id,
                        $"{Context.User.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should be " +
                        $"added with the reply of `{Reply}`.");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` was suggested!")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"use `{BotConfiguration.Prefix}ccalias add` to add an alias to the command! \n" +
                            "Please note, to make the command ping a user if mentioned, add `USER` to the reply~! \n" +
                            "To make the command ping a user if mentioned, add `AUTHOR` to the reply. \n" +
                            $"To modify the reply at any time, use `{BotConfiguration.Prefix}ccedit`.")
                        .SendEmbed(Context.Channel);

                    break;
                case ActionType.Edit:
                    if (string.IsNullOrEmpty(Reply)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Edit Reply.")
                            .WithDescription("Reply is not given! Please enter a reply with this command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (Command == null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Edit Reply.")
                            .WithDescription($"A command with the name `{BotConfiguration.Prefix}{CommandName}` doesn't exist!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (Reply.Length > 1000) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Edit Reply.")
                            .WithDescription($"Heya! Please cut down on your length of reply. " +
                                $"It should be a maximum of 1000 characters. Currently this character count sits at {Reply.Length}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await SendForAdminApproval(EditCommandCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Reply", Reply }
                        },
                        Context.User.Id,
                        $"{Context.User.GetUserInformation()} has suggested that the command {BotConfiguration.Prefix}{CommandName} should be " +
                        $"edited from `{Command.Reply}` to `{Reply}`");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The edit to `{BotConfiguration.Prefix}{CommandName}` was suggested!")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"The command `{BotConfiguration.Prefix}{CommandName}` will be changed to have the reply of `{Reply}` rather than `{Command.Reply}`.")
                        .SendEmbed(Context.Channel);

                    break;
                case ActionType.Remove:
                    await SendForAdminApproval(RemoveCommandCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                        },
                        Context.User.Id,
                        $"{Context.User.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should be removed!"
                    );

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The removal of `{BotConfiguration.Prefix}{CommandName}` was suggested.")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"The command `{BotConfiguration.Prefix}{CommandName}` will be removed from the database.")
                        .SendEmbed(Context.Channel);
                    break;
                case ActionType.Get:
                    CustomCommand GetCommand = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (GetCommand == null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable To Get Reply.")
                            .WithDescription("A command with the name `{BotConfiguration.Prefix}{CommandName}` doesn't exist!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{BotConfiguration.Prefix}{CommandName}")
                        .AddField("Reply", GetCommand.Reply)
                        .AddField("Aliases", string.Join('\n', JsonConvert.DeserializeObject<List<string>>(GetCommand.Alias)))
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable To Modify Reply.")
                        .WithDescription($"The {ActionType} does not exist as an option!")
                        .SendEmbed(Context.Channel);
                    return;
            }
        }

        /// <summary>
        /// The CreateCommandCallback runs on the confirmation of the admins approving a custom command.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to add.
        ///     Reply = The reply of the given command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public void CreateCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Reply = Parameters["Reply"];

            CustomCommandDB.CustomCommands.Add(new CustomCommand() {
                CommandName = CommandName,
                Reply = Reply,
                Alias = ""
            });

            CustomCommandDB.SaveChanges();
        }

        /// <summary>
        /// The EditCommandCallback runs on the confirmation of the admins approving the editing of a custom command.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to edit.
        ///     Reply = The edited reply of the given command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public void EditCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Reply = Parameters["Reply"];

            CustomCommandDB.GetCommandByNameOrAlias(CommandName).Reply = Reply;

            CustomCommandDB.SaveChanges();
        }

        /// <summary>
        /// The RemoveCommandCallback runs on the confirmation of the admins approving the removal of a custom command.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to remove.
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public void RemoveCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];

            CustomCommandDB.CustomCommands.Remove(CustomCommandDB.GetCommandByNameOrAlias(CommandName));

            CustomCommandDB.SaveChanges();
        }

    }

}

using Dexter.Configurations;
using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dexter.Commands {

    public partial class CustomCommands {

        /// <summary>
        /// The CustomCommand method runs on CC and will add or edit a custom command in the CustomCommandDB
        /// based on the given CMDActionType and apply the REPLY parameter to it.
        /// </summary>
        /// <param name="CMDActionType">The CMDActionType specifies whether the action is to add or edit the command in the database.</param>
        /// <param name="CommandName">The CommandName specifies the command that you wish to add or edit from the database.</param>
        /// <param name="Reply">The Reply specifies the reply that you wish to given command to have.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("cc")]
        [Summary("Applies an action to a customizeable command.")]
        [Alias("customcommand", "command")]
        [RequireModerator]

        public async Task CustomCommand (CMDActionType CMDActionType, string CommandName, [Remainder] string Reply) {
            switch (CMDActionType) {
                case CMDActionType.Add:
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
                        $"added with the reply of `{Reply}`.");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{CommandName}` was suggested!")
                        .WithDescription($"Once it has passed admin approval, " +
                            $"use `{BotConfiguration.Prefix}ccalias add` to add an alias to the command! \n" +
                            "Please note, to make the command ping a user if mentioned, add `USER` to the reply~! \n" +
                            $"To modify the reply at any time, use `{BotConfiguration.Prefix}ccedit`")
                        .SendEmbed(Context.Channel);

                    break;
                case CMDActionType.Edit:
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
                            $"The command `{BotConfiguration.Prefix}{CommandName}` will be changed to have the reply of `{Reply}` rahter than `{Command.Reply}`.")
                        .SendEmbed(Context.Channel);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }
        }


        /// <summary>
        /// The CustomCommand method runs on CC and will remove or get a custom command from the CustomCommandDB based on the given CMDActionType.
        /// </summary>
        /// <param name="CMDActionType">The CMDActionType specifies whether the action is to remove or get the command from the database.</param>
        /// <param name="CommandName">The CommandName specifies the command that you wish to remove or get from the database.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("cc")]
        [Summary("Applies an action to a customizeable command.")]
        [Alias("customcommand", "command")]
        [RequireModerator]

        public async Task CustomCommand(CMDActionType CMDActionType, string CommandName) {
            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            if (Command == null)
                throw new InvalidOperationException($"A command with the name `{BotConfiguration.Prefix}{CommandName}` doesn't exist!");

            switch (CMDActionType) {
                case CMDActionType.Remove:
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
                case CMDActionType.Get:
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{BotConfiguration.Prefix}{CommandName}")
                        .AddField("Reply", Command.Reply)
                        .AddField("Aliases", string.Join('\n', JsonConvert.DeserializeObject<List<string>>(Command.Alias)))
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }
        }

        /// <summary>
        /// The CreateCommandCallback runs on the confirmation of the admins approving a custom command.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to add.
        ///     Reply = The reply of the given command.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

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

        /// <summary>
        /// The EditCommandCallback runs on the confirmation of the admins approving the editing of a custom command.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to edit.
        ///     Reply = The edited reply of the given command.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        public async Task EditCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Reply = Parameters["Reply"];

            CustomCommandDB.GetCommandByNameOrAlias(CommandName).Reply = Reply;

            await CustomCommandDB.SaveChangesAsync();
        }

        /// <summary>
        /// The RemoveCommandCallback runs on the confirmation of the admins approving the removal of a custom command.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to remove.
        /// </param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        public async Task RemoveCommandCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];

            CustomCommandDB.CustomCommands.Remove(CustomCommandDB.GetCommandByNameOrAlias(CommandName));

            await CustomCommandDB.SaveChangesAsync();
        }

    }

}

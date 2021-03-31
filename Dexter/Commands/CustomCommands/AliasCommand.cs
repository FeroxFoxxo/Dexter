using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class CustomCommands {

        /// <summary>
        /// The Alias method runs on CCALIAS and will add or remove an alias from a command by the given AliasType
        /// to/from the CustomCommandDB based to the command provided by the CommandName string.
        /// It will also list all command aliases.
        /// </summary>
        /// <param name="AliasActionType">The AliasActionType specifies whether the action is to add or remove the alias from the command.</param>
        /// <param name="CommandName">The CommandName specifies the command that you want the alias to be applied to.</param>
        /// <param name="Alias">The Alias is the string of the alias that you wish to be applied to the command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("ccalias")]
        [Summary("Modifies a custom commands aliases.\n" +
            "`ADD [COMMAND NAME] [ALIAS]` - adds an alias to a given command.\n" +
            "`REMOVE [COMMAND NAME] [ALIAS]` - removes an alias from a given command.\n" +
            "`LIST [COMMAND NAME]` - lists all the aliases of a command."
        )]
        [Alias("ccaka")]
        [RequireModerator]

        public async Task Alias (AliasActionType AliasActionType, string CommandName, [Optional] string Alias) {
            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            switch (AliasActionType) {
                case AliasActionType.Add:
                    if (string.IsNullOrEmpty(Alias)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Adding Alias.")
                            .WithDescription("Alias is not given! Please enter an alias with this command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    CustomCommand Add = CustomCommandDB.GetCommandByNameOrAlias(Alias);

                    if (Add != null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Adding Alias.")
                            .WithDescription($"The command `{BotConfiguration.Prefix}{Add.CommandName}` " +
                            $"already has the alias `{BotConfiguration.Prefix}{Alias}`!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await SendForAdminApproval(AddAliasCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Alias", Alias }
                        },
                        Context.User.Id,
                        $"{Context.User.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should have the alias `{BotConfiguration.Prefix}{Alias}` added to it!");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` was suggested to have added the alias of `{BotConfiguration.Prefix}{Alias}`!")
                        .WithDescription($"Once the suggestion has passed admin approval, you may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.Remove:
                    if (string.IsNullOrEmpty(Alias)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Removing Alias.")
                            .WithDescription("Alias is not given! Please enter an alias with this command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    CustomCommand Remove = CustomCommandDB.GetCommandByNameOrAlias(Alias);

                    if (Remove == null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Removing Alias.")
                            .WithDescription($"No command with alias of `{BotConfiguration.Prefix}{Alias}` exists! Are you sure you spelt it correctly?")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await SendForAdminApproval(AddAliasCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Alias", Alias }
                        },
                        Context.User.Id,
                        $"{Context.User.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should have the alias `{BotConfiguration.Prefix}{Alias}` removed from it!");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` was suggested to be remved from the alias of `{BotConfiguration.Prefix}{Alias}`!")
                        .WithDescription($"Once the suggestion has passed admin approval, you may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.List:
                    CustomCommand List = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (List == null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Listing Alias.")
                            .WithDescription($"The command `{BotConfiguration.Prefix}{CommandName}` doesn't exist!")
                            .SendEmbed(Context.Channel);
                        return;

                    }

                    List<string> AliasList = JsonConvert.DeserializeObject<List<string>>(List.Alias);

                    string Aliases = string.Join('\n', AliasList.Select(Alias => $"{BotConfiguration.Prefix}{Alias}"));

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` has these aliases:")
                        .WithDescription(AliasList.Count > 0 ? Aliases : "No aliases set!")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable To Modify Alias.")
                        .WithDescription($"The {AliasActionType} does not exist as an option!")
                        .SendEmbed(Context.Channel);
                    break;
            }
        }

        /// <summary>
        /// The AddAliasCallback runs on the confirmation of the admins approving a given alias addition.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to add the alias to.
        ///     Alias = The alias you wish to add to the command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public void AddAliasCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Alias = Parameters["Alias"];

            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            List<string> AddedAlias = new ();

            if (!string.IsNullOrEmpty(Command.Alias))
                JsonConvert.DeserializeObject<List<string>>(Command.Alias);

            AddedAlias.Add(Alias);

            Command.Alias = JsonConvert.SerializeObject(AddedAlias);

            CustomCommandDB.SaveChanges();
        }

        /// <summary>
        /// The RemoveAliasCallback runs on the confirmation of the admins approving a given alias removal.
        /// </summary>
        /// <param name="Parameters">The called back parameters:
        ///     CommandName = The name of the command you wish to remove the alias from.
        ///     Alias = The alias you wish to remove from the command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public void RemoveAliasCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Alias = Parameters["Alias"];

            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            List<string> RemovedAlias = JsonConvert.DeserializeObject<List<string>>(Command.Alias);

            RemovedAlias.Remove(Alias);

            Command.Alias = JsonConvert.SerializeObject(RemovedAlias);

            CustomCommandDB.SaveChanges();
        }

    }

}

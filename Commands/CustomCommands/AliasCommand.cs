using Dexter.Attributes;
using Dexter.Configurations;
using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class CustomCommands {

        [Command("ccalias")]
        [Summary("Creates a new customizeable command.")]
        [Alias("ccaka")]
        [RequireModerator]

        public async Task AliasCommandAsync(AliasActionType AliasActionType, string CommandName, string Alias) {
            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            switch (AliasActionType) {
                case AliasActionType.Add:
                    CustomCommand Add = CustomCommandDB.GetCommandByNameOrAlias(Alias);

                    if (Add != null)
                        throw new InvalidOperationException($"The command `{BotConfiguration.Prefix}{Add.CommandName}` " +
                            $"already has the alias `{BotConfiguration.Prefix}{Alias}`!");

                    await SendForAdminApproval(AddAliasCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Alias", Alias }
                        },
                        Context.Message.Author.Id,
                        $"{Context.Message.Author.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should have the alias `{BotConfiguration.Prefix}{Alias}` added to it!");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` was suggested to have added the alias of `{BotConfiguration.Prefix}{Alias}`!")
                        .WithDescription($"Once the suggestion has passed admin approval, you may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.Remove:
                    CustomCommand Remove = CustomCommandDB.GetCommandByNameOrAlias(Alias);

                    if (Remove == null)
                        throw new InvalidOperationException($"No command with alias of `{BotConfiguration.Prefix}{Alias}` exists! Are you sure you spelt it correctly?");

                    await SendForAdminApproval(AddAliasCallback,
                        new Dictionary<string, string>() {
                            { "CommandName", CommandName.ToLower() },
                            { "Alias", Alias }
                        },
                        Context.Message.Author.Id,
                        $"{Context.Message.Author.GetUserInformation()} has suggested that the command `{BotConfiguration.Prefix}{CommandName}` should have the alias `{BotConfiguration.Prefix}{Alias}` removed from it!");

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` was suggested to be remved from the alias of `{BotConfiguration.Prefix}{Alias}`!")
                        .WithDescription($"Once the suggestion has passed admin approval, you may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(AliasActionType.ToString());
            }
        }

        public async Task AddAliasCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Alias = Parameters["Alias"];

            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            List<string> AddedAlias = JsonConvert.DeserializeObject<List<string>>(Command.Alias);

            AddedAlias.Add(Alias);

            Command.Alias = JsonConvert.SerializeObject(AddedAlias);

            await CustomCommandDB.SaveChangesAsync();
        }

        public async Task RemoveAliasCallback(Dictionary<string, string> Parameters) {
            string CommandName = Parameters["CommandName"];
            string Alias = Parameters["Alias"];

            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            List<string> RemovedAlias = JsonConvert.DeserializeObject<List<string>>(Command.Alias);

            RemovedAlias.Remove(Alias);

            Command.Alias = JsonConvert.SerializeObject(RemovedAlias);

            await CustomCommandDB.SaveChangesAsync();
        }

        [Command("ccalias")]
        [Summary("Creates a new customizeable command.")]
        [Alias("ccaka", "ccother")]
        [RequireModerator]

        public async Task AliasCommandAsync(AliasActionType AliasAction, string CommandName) {
            switch (AliasAction) {
                case AliasActionType.List:
                    CustomCommand List = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (List == null)
                        throw new InvalidOperationException($"The command `{BotConfiguration.Prefix}{CommandName}` doesn't exist!");

                    List<string> AliasList = JsonConvert.DeserializeObject<List<string>>(List.Alias);

                    string Aliases = string.Join('\n', AliasList.Select(Alias => $"{BotConfiguration.Prefix}{Alias}"));

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{BotConfiguration.Prefix}{CommandName}` has these aliases:")
                        .WithDescription(AliasList.Count > 0 ? Aliases : "No aliases set!")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.Add:
                case AliasActionType.Remove:
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Bad argument count.")
                        .WithDescription($"Please specify which command you wish to {AliasAction.ToString().ToLower()} this alias from! <3")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(AliasAction.ToString());
            }
        }
    }
}

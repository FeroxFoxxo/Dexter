using Dexter.Configurations;
using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.CustomCommands;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class CustomCommands {

        [Command("ccalias")]
        [Summary("Creates a new customizeable command.")]
        [Alias("ccaka", "ccother")]
        [RequireModerator]

        public async Task AliasCommandAsync(AliasActionType AliasAction, string CommandName, string Alias) {
            CustomCommand Command = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

            switch (AliasAction) {
                case AliasActionType.Add:
                    CustomCommand Add = CustomCommandDB.GetCommandByNameOrAlias(Alias);

                    if (Add != null)
                        throw new InvalidOperationException($"The command `{Add.CommandName}` already has the alias `{Alias}`!");

                    List<string> Aliases = Command.Alias.Split(',').ToList();

                    Aliases.Add(Alias);

                    Command.Alias = string.Join(',', Aliases);

                    await CustomCommandDB.SaveChangesAsync();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{CommandName}` was given the alias of `{Alias}`!")
                        .WithDescription($"Use `{BotConfiguration.Prefix}ccalias remove` to remove an alias from this command! \n" +
                            $"You may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.Remove:
                    CustomCommand Remove = CustomCommandDB.GetCommandByNameOrAlias(Alias);

                    if (Remove == null)
                        throw new InvalidOperationException($"No command with alias of `{Alias}` exists! Are you sure you spelt it correctly?");

                    List<string> Aliases2 = Command.Alias.Split(',').ToList();

                    Aliases2.Remove(Alias);

                    Command.Alias = string.Join(',', Aliases2);

                    await CustomCommandDB.SaveChangesAsync();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{CommandName}` was removed from the alias of `{Alias}`!")
                        .WithDescription($"Use `{BotConfiguration.Prefix}ccalias add` to add an alias to this command! \n" +
                            $"You may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(AliasAction.ToString());
            }
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
                        throw new InvalidOperationException($"The command `{CommandName}` doesn't exist!");

                    string Aliases = string.Join('\n', List.Alias.Split(","));

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The command `{CommandName}` has these aliases:")
                        .WithDescription(Aliases.Replace("\n", "").Length > 0 ? Aliases : "No aliases set!")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.Add:
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Bad argument count!")
                        .WithDescription("Please specify which command you would want to add this alias to! <3")
                        .SendEmbed(Context.Channel);
                    break;
                case AliasActionType.Remove:
                    CustomCommand Remove = CustomCommandDB.GetCommandByNameOrAlias(CommandName);

                    if (Remove == null)
                        throw new InvalidOperationException($"No command with alias of `{CommandName}` exists! Are you sure you spelt it correctly?");

                    List<string> Aliases2 = Remove.Alias.Split(',').ToList();

                    Aliases2.Remove(CommandName);

                    Remove.Alias = string.Join(',', Aliases2);

                    await CustomCommandDB.SaveChangesAsync();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"The alias `{CommandName}` was removed as an alias of `{Remove.CommandName}`!")
                        .WithDescription($"Use `{BotConfiguration.Prefix}ccalias add` to add an alias to this command! \n" +
                            $"You may use the `{BotConfiguration.Prefix}ccalias list` command to view the aliases of this command.")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(AliasAction.ToString());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Dexter.Commands
{

    public partial class UtilityCommands
    {

        /// <summary>
        /// Displays a navigable list of commands. This list is built from the "Summary" attributes on each command.
        /// </summary>
        /// <param name="Command">
        /// An optional parameter which will display extended information for a specific command if non-null.
        /// The information for this command is pulled from the "ExtendedSummary" attribute (default to "Summary").
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        [Command("help")]
        [Summary("Displays all avaliable commands.")]
        [Alias("commands")]
        [BotChannel]

        public async Task HelpCommand([Optional][Remainder] string Command)
        {
            if (string.IsNullOrEmpty(Command))
            {
                List<EmbedBuilder> Embeds = new();

                Embeds.Add(
                    BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{DiscordSocketClient.CurrentUser.Username} Help")
                        .WithDescription($"{BotConfiguration.Help}")
                );

                ServiceCollection ServiceCollection = new();

                HelpAbstraction HelpAbstraction = new()
                {
                    BotConfiguration = BotConfiguration,
                    DiscordSocketClient = DiscordSocketClient
                };

                ServiceCollection.AddSingleton(HelpAbstraction);

                foreach (ModuleInfo Module in CommandService.Modules)
                {
                    string ModuleName = Regex.Replace(Module.Name, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]);

                    EmbedBuilder CurrentBuilder = BuildEmbed(EmojiEnum.Love).WithTitle(ModuleName);

                    string Description = string.Empty;

                    foreach (CommandInfo CommandInfo in Module.Commands)
                    {
                        PreconditionResult Result = await CommandInfo.CheckPreconditionsAsync(Context, ServiceCollection.BuildServiceProvider());

                        if (Result.IsSuccess && !string.IsNullOrWhiteSpace(CommandInfo.Summary))
                        {
                            string Field = $"**{BotConfiguration.Prefix}{string.Join("/", CommandInfo.Aliases.ToArray())}:** {CommandInfo.Summary}\n\n";

                            try
                            {
                                CurrentBuilder.WithDescription(Description += Field);
                            }
                            catch (Exception)
                            {
                                Embeds.Add(CurrentBuilder);
                                Description = string.Empty;
                                CurrentBuilder = BuildEmbed(EmojiEnum.Unknown).WithTitle(ModuleName).WithDescription(Description += Field);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(CurrentBuilder.Description))
                        Embeds.Add(CurrentBuilder);
                }

                List<string> Pages = new();
                string PreviousPage = $"{DiscordSocketClient.CurrentUser.Username} Help";
                int PageNumber = 0;

                foreach (EmbedBuilder Embed in Embeds)
                {
                    PageNumber++;
                    if (PreviousPage != Embed.Title)
                    {
                        Pages.Add($"**Page {PageNumber}:** {Embed.Title}");
                        PreviousPage = Embed.Title;
                    }
                }

                Embeds[0].AddField("Help Pages",
                    string.Join('\n', Pages.ToArray())
                );

                CreateReactionMenu(Embeds.ToArray(), Context.Channel);
            }
            else
            {
                SearchResult Result = CommandService.Search(Context, Command);

                if (!Result.IsSuccess)
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unknown Command")
                        .WithDescription($"Sorry, I couldn't find a command like **{Command}**.")
                        .SendEmbed(Context.Channel);
                else
                {
                    EmbedBuilder EmbedBuilder = BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{BotConfiguration.Prefix}{Command} Command Help");

                    foreach (CommandMatch CommandMatch in Result.Commands)
                        EmbedBuilder.GetParametersForCommand(CommandMatch.Command, BotConfiguration);

                    await EmbedBuilder.SendEmbed(Context.Channel);
                }
            }
        }

    }

}

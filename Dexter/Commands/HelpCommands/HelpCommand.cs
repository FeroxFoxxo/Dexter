using Dexter.Abstractions;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class HelpCommands {

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

        public async Task HelpCommand([Optional] [Remainder] string Command) {
            if(string.IsNullOrEmpty(Command)) {
                List<EmbedBuilder> EmbedBuilders = new();

                int PageNumber = 2;

                EmbedBuilders.Add(
                    BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{DiscordSocketClient.CurrentUser.Username} Help")
                        .WithDescription($"{BotConfiguration.Help}")
                );

                List<string> Pages = new();

                foreach (ModuleInfo Module in CommandService.Modules) {
                    string ModuleName = Regex.Replace(Module.Name, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]);

                    List<string> Description = new();

                    ServiceCollection ServiceCollection = new();

                    HelpAbstraction HelpAbstraction = new() {
                        BotConfiguration = BotConfiguration,
                        DiscordSocketClient = DiscordSocketClient
                    };

                    ServiceCollection.AddSingleton(HelpAbstraction);

                    foreach (CommandInfo CommandInfo in Module.Commands) {
                        PreconditionResult Result = await CommandInfo.CheckPreconditionsAsync(Context, ServiceCollection.BuildServiceProvider());

                        if (Result.IsSuccess)
                            Description.Add($"**~{string.Join("/", CommandInfo.Aliases.ToArray())}:** {CommandInfo.Summary}");
                    }

                    if (Description.Count > 0) {
                        Pages.Add($"**Page {PageNumber++}:** {ModuleName}");
                        EmbedBuilders.Add(
                            BuildEmbed(EmojiEnum.Love)
                                .WithTitle($"{ModuleName}")
                                .WithDescription(string.Join("\n\n", Description.ToArray()))
                        );
                    }
                }

                EmbedBuilders[0].AddField("Help Pages",
                    string.Join('\n', Pages.ToArray())
                );

                await CreateReactionMenu(EmbedBuilders.ToArray(), Context.Channel);
            } else {
                SearchResult Result = CommandService.Search(Context, Command);

                if (!Result.IsSuccess)
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unknown Command")
                        .WithDescription($"Sorry, I couldn't find a command like **{Command}**.")
                        .SendEmbed(Context.Channel);
                else {
                    EmbedBuilder EmbedBuilder = BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Here are some commands like **{Command}**!");

                    foreach (CommandMatch CommandMatch in Result.Commands)
                        EmbedBuilder.GetParametersForCommand(CommandMatch.Command, BotConfiguration);

                    await EmbedBuilder.SendEmbed(Context.Channel);
                }
            }
        }

    }

}

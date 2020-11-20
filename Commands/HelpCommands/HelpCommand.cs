using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class HelpCommands {

        [Command("help")]
        [Summary("Displays all avaliable commands.")]
        [Alias("helpme", "pleasehelp", "how2use", "howtouse")]
        public async Task HelpCommand() {
            EmbedBuilder Embed = BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Hiya, I'm {Context.Client.CurrentUser.Username}~! Here's a list of modules and commands you can use!")
                .WithDescription($"Use {BotConfiguration.Prefix}help [commandName] to show information about a command.");

            foreach (ModuleInfo Module in CommandService.Modules) {
                List<string> Description = new ();

                foreach (CommandInfo CommandInfo in Module.Commands) {
                    PreconditionResult Result = await CommandInfo.CheckPreconditionsAsync(Context);

                    if (!Description.Contains($"{BotConfiguration.Prefix}{CommandInfo.Aliases[0]}")) {
                        if (Result.IsSuccess)
                            Description.Add($"{BotConfiguration.Prefix}{CommandInfo.Aliases[0]}");
                    } else continue;
                }

                if (Description.Count > 0)
                    Embed.AddField(Regex.Replace(Module.Name, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]), string.Join("\n", Description.ToArray()), false);
            }

            await Embed.SendEmbed(Context.Channel);
        }

        [Command("help")]
        [Summary("Displays detailed information about a command.")]
        [Alias("helpme", "pleasehelp", "how2use", "howtouse")]
        public async Task HelpCommand(string Command) {
            SearchResult Result = CommandService.Search(Context, Command);

            if (!Result.IsSuccess)
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Command")
                    .WithDescription($"Sorry, I couldn't find a command like **{Command}**.")
                    .SendEmbed(Context.Channel);
            else await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Here are some commands like **{Command}**!")
                .GetParametersForCommand(CommandService, Command)
                .SendEmbed(Context.Channel);
        }

    }
}

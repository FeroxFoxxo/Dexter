using Dexter.Core.Abstractions;
using Dexter.Core.DiscordApp;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("help")]
        [Summary("Displays all avaliable commands.")]
        [Alias("helpme", "help me", "help-me", "pleasehelp", "please help", "how2use", "howtouse", "how 2 use", "how to use")]
        public async Task HelpCommand() {
            List<EmbedFieldBuilder> Fields = new List<EmbedFieldBuilder>();

            foreach (ModuleInfo Module in CommandHandler.CommandService.Modules) {
                List<string> Description = new List<string>();

                foreach (CommandInfo CommandInfo in Module.Commands) {
                    PreconditionResult Result = await CommandInfo.CheckPreconditionsAsync(Context);

                    if (Description.Contains($"~{CommandInfo.Aliases.First()}"))
                        continue;
                    else if (Result.IsSuccess)
                        Description.Add($"~{CommandInfo.Aliases.First()}");
                }

                if (Description.Count != 0)
                    Fields.Add(new EmbedFieldBuilder {
                        Name = Regex.Replace(Module.Name, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]),
                        Value = string.Join("\n", Description.ToArray())
                    });
            }

            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Hiya, I'm {Context.BotConfiguration.Bot_Name}~! Here's a list of modules and commands you can use!")
                .WithDescription("Use ~help [commandName] to show information about a command!")
                .WithFields(Fields.ToArray())
                .SendEmbed(Context.Channel);
        }

        [Command("help")]
        [Summary("Displays detailed information about a command.")]
        [Alias("helpme", "help me", "help-me", "pleasehelp", "please help", "how2use", "howtouse", "how 2 use", "how to use")]
        public async Task HelpCommand(string Command) {
            SearchResult Result = CommandHandler.CommandService.Search(Context, Command);

            if (!Result.IsSuccess)
                await Context.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Command")
                    .WithDescription($"Sorry, I couldn't find a command like **{Command}**.")
                    .SendEmbed(Context.Channel);
            else await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Here are some commands like **{Command}**!")
                .WithFields(CommandHandler.GetParametersForCommand(Command))
                .SendEmbed(Context.Channel);
        }

    }
}

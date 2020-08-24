using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Dexter.Core.Enums;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public class HelpCommands : AbstractModule {
        private readonly CommandService Service;

        public HelpCommands(CommandService _Service, BotConfiguration _BotConfiguration) : base(_BotConfiguration) {
            Service = _Service;
        }

        [Command("help")]
        [Summary("Displays all avaliable commands.")]
        public async Task HelpCommand() {
            List<EmbedFieldBuilder> Fields = new List<EmbedFieldBuilder>();

            foreach (ModuleInfo Module in Service.Modules) {
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

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Hiya, I'm {BotConfiguration.Bot_Name}~! Here's a list of modules and commands you can use!")
                .WithDescription("Use ~help [commandName] to show information about a command!")
                .WithFields(Fields.ToArray())
                .SendEmbed(Context.Channel);
        }

        [Command("help")]
        [Summary("Displays detailed information about a command.")]
        public async Task HelpCommand(string Command) {
            SearchResult Result = Service.Search(Context, Command);

            if (!Result.IsSuccess) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Command")
                    .WithDescription($"Sorry, I couldn't find a command like **{Command}**.")
                    .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Here are some commands like **{Command}**!")
                .WithFields(GetParametersForCommand(Command))
                .SendEmbed(Context.Channel);
        }

        public async Task SendCommandError(Optional<CommandInfo> Command, ICommandContext Context, IResult Result) {
            if (Result.IsSuccess)
                return;

            switch (Result.Error) {
                case CommandError.BadArgCount:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("You've entered an invalid amount of parameters for this command!")
                        .WithDescription($"Here are some options of parameters you can have for the command **{Command.Value.Name}**.")
                        .WithFields(GetParametersForCommand(Command.Value.Name))
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnmetPrecondition:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Access Denied")
                        .WithDescription("Hiya! It seems like you don't have access to this command. Please check that you have the required roles to run this command.")
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnknownCommand:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unknown Command")
                        .WithDescription($"Oopsies! It seems as if the command **{Context.Message.Content.Split(' ')[0]}** doesn't exist!")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    if (Result is ExecuteResult executeResult)
                        await BuildEmbed(EmojiEnum.Annoyed)
                         .WithTitle(Regex.Replace(executeResult.Exception.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                         .WithDescription(executeResult.Exception.Message)
                         .SendEmbed(Context.Channel);
                    else
                        await BuildEmbed(EmojiEnum.Annoyed)
                         .WithTitle(Regex.Replace(Result.Error.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                         .WithDescription(Result.ErrorReason)
                         .SendEmbed(Context.Channel);
                    break;
            }
        }

        private EmbedFieldBuilder[] GetParametersForCommand(string Command) {
            List<EmbedFieldBuilder> Fields = new List<EmbedFieldBuilder>();
            SearchResult Result = Service.Search(Context, Command);

            foreach (CommandMatch Match in Result.Commands) {
                CommandInfo CommandInfo = Match.Command;

                string CommandDescription = $"Parameters: {string.Join(", ", CommandInfo.Parameters.Select(p => p.Name))}";

                if (CommandInfo.Parameters.Count > 0)
                    CommandDescription = $"Parameters: {string.Join(", ", CommandInfo.Parameters.Select(p => p.Name))}";
                else
                    CommandDescription = "No parameters";

                if (!string.IsNullOrEmpty(CommandInfo.Summary))
                    CommandDescription += $"\nSummary: {CommandInfo.Summary}";

                Fields.Add(new EmbedFieldBuilder {
                    Name = string.Join(", ", CommandInfo.Aliases),
                    Value = CommandDescription,
                    IsInline = false
                });
            }

            return Fields.ToArray();
        }
    }
}

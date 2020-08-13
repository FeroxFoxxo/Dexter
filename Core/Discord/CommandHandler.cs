using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class CommandHandler {
        private readonly DiscordBot Discord;

        private IServiceProvider Services;
        private CommandService CommandService;

        public CommandHandler(DiscordBot _Discord) {
            Discord = _Discord;
        }

        public async Task InitializeAsync() {
            CommandService = new CommandService();

            CommandService.CommandExecuted += OnCommandExecutedAsync;
            Discord.Client.MessageReceived += HandleCommandAsync;

            ServiceCollection Collection = new ServiceCollection();

            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(x))
                .ToList()
                .ForEach((x) => Collection.AddSingleton(x));

            Services = Collection.BuildServiceProvider();

            _ = await CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }

        private async Task HandleCommandAsync(SocketMessage SocketMessage) {
            if (!(SocketMessage is SocketUserMessage Message))
                return;

            int ArgumentPosition = 0;

            if ((Message.HasMentionPrefix(Discord.Client.CurrentUser, ref ArgumentPosition) || Message.HasCharPrefix('~', ref ArgumentPosition)) && !Message.Author.IsBot)
                await CommandService.ExecuteAsync(new SocketCommandContext(Discord.Client, Message), ArgumentPosition, Services);
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> Command, ICommandContext Context, IResult Result) {
            if (Result.IsSuccess)
                return;

            switch (Result.Error) {
                case CommandError.BadArgCount:
                    List<EmbedFieldBuilder> Fields = new List<EmbedFieldBuilder>();

                    SearchResult SearchResult = CommandService.Search(Context, Command.Value.Name);

                    foreach (CommandMatch Match in SearchResult.Commands) {
                        CommandInfo CommandInfo = Match.Command;

                        string Description = "Parameters: " + string.Join(", ", CommandInfo.Parameters.Select(p => p.Name));

                        if (CommandInfo.Parameters.Count > 0)
                            Description = "Parameters: " + string.Join(", ", CommandInfo.Parameters.Select(p => p.Name));
                        else
                            Description = "No parameters";

                        if (!string.IsNullOrEmpty(CommandInfo.Summary))
                            Description += "\nSummary: " + CommandInfo.Summary;

                        Fields.Add(new EmbedFieldBuilder {
                            Name = string.Join(", ", CommandInfo.Aliases),
                            Value = Description,
                            IsInline = false
                        });
                    }

                    await AbstractModule.BuildEmbed()
                        .WithTitle("You've entered an invalid amount of parameters for this command!")
                        .WithDescription("Here are some options of parameters you can have for the **" + Command.Value.Name + "** command:")
                        .WithFields(Fields.ToArray())
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnmetPrecondition:
                    await AbstractModule.BuildEmbed()
                        .WithTitle("Access Denied")
                        .WithDescription("Hiya! It seems like you don't have access to this command. Please check that you have the role required to run this command!")
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnknownCommand:
                    await AbstractModule.BuildEmbed()
                        .WithTitle("Unknown Command")
                        .WithDescription("Oopsies! It seems as if the command " + Command.Value.Name + " doesn't exist!")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    if (Result is ExecuteResult executeResult)
                        await AbstractModule.BuildEmbed()
                         .WithTitle(Regex.Replace(executeResult.Exception.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                         .WithDescription(executeResult.Exception.Message)
                         .SendEmbed(Context.Channel);
                    else
                        await AbstractModule.BuildEmbed()
                         .WithTitle(Regex.Replace(Result.Error.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                         .WithDescription(Result.ErrorReason)
                         .SendEmbed(Context.Channel);
                    break;
            }
        }
    }
}

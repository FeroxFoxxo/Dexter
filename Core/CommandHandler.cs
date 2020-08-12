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
        private readonly DexterDiscord Discord;
        private readonly IServiceProvider Services;
        private readonly CommandService CommandService;

        public CommandHandler(DexterDiscord _Discord) {
            Discord = _Discord;
            CommandService = new CommandService();

            ServiceCollection Collection = new ServiceCollection();

            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(x))
                .ToList()
                .ForEach((x) => Collection.AddSingleton(x));

            Services = Collection.BuildServiceProvider();
        }

        public async Task InitializeAsync() {
            Discord.Client.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += OnCommandExecutedAsync;

            _ = await CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }

        private async Task HandleCommandAsync(SocketMessage s) {
            if (!(s is SocketUserMessage msg))
                return;

            var argPos = 0;

            if ((msg.HasMentionPrefix(Discord.Client.CurrentUser, ref argPos) || msg.HasCharPrefix('~', ref argPos)) && !msg.Author.IsBot) {
                var context = new SocketCommandContext(Discord.Client, msg);
                await CommandService.ExecuteAsync(context, argPos, Services);
            }
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> Command, ICommandContext Context, IResult Result) {
            if (Result.IsSuccess)
                return;

            switch (Result.Error) {
                case CommandError.BadArgCount:
                    List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                    SearchResult SearchResult = CommandService.Search(Context, Command.Value.Name);

                    foreach (var match in SearchResult.Commands) {
                        var cmd = match.Command;

                        string cmdDescription = "Parameters: " + string.Join(", ", cmd.Parameters.Select(p => p.Name));

                        if (cmd.Parameters.Count > 0)
                            cmdDescription = "Parameters: " + string.Join(", ", cmd.Parameters.Select(p => p.Name));
                        else
                            cmdDescription = "No parameters";

                        if (!string.IsNullOrEmpty(cmd.Summary))
                            cmdDescription += "\nSummary: " + cmd.Summary;

                        fields.Add(new EmbedFieldBuilder {
                            Name = string.Join(", ", cmd.Aliases),
                            Value = cmdDescription,
                            IsInline = false
                        });
                    }

                    await Module.BuildEmbed()
                        .WithTitle("You've entered an invalid amount of parameters for this command!")
                        .WithDescription("Here are some options of parameters you can have for the **" + Command.Value.Name + "** command:")
                        .WithFields(fields.ToArray())
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnmetPrecondition:
                    await Module.BuildEmbed()
                        .WithTitle("Access Denied")
                        .WithDescription("Hiya! It seems like you don't have access to this command. Please check that you have the role required to run this command!")
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnknownCommand:
                    await Module.BuildEmbed()
                        .WithTitle("Unknown Command")
                        .WithDescription("Oopsies! It seems as if the command " + Command.Value.Name + " doesn't exist!")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    if (Result is ExecuteResult executeResult)
                        await Module.BuildEmbed()
                         .WithTitle(Regex.Replace(executeResult.Exception.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                         .WithDescription(executeResult.Exception.Message)
                         .SendEmbed(Context.Channel);
                    else
                        await Module.BuildEmbed()
                         .WithTitle(Regex.Replace(Result.Error.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                         .WithDescription(Result.ErrorReason)
                         .SendEmbed(Context.Channel);
                    break;
            }
        }
    }
}

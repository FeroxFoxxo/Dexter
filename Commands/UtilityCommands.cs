using Dexter.Core;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public class UtilityCommands : Module {
        private readonly CommandService Service;

        public UtilityCommands(CommandService _Service) {
            Service = _Service;
        }

        [Command("help")]
        [Summary("Displays all avaliable commands.")]
        public async Task HelpCommand() {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

            foreach (ModuleInfo module in Service.Modules) {
                List<string> description = new List<string>();

                foreach (CommandInfo cmd in module.Commands) {
                    PreconditionResult result = await cmd.CheckPreconditionsAsync(Context);

                    if (description.Contains("~" + cmd.Aliases.First()))
                        continue;

                    if (result.IsSuccess)
                        description.Add("~" + cmd.Aliases.First());
                }

                if (description.Count != 0)
                    fields.Add(new EmbedFieldBuilder {
                        Name = Regex.Replace(module.Name, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]),
                        Value = string.Join("\n", description.ToArray())
                    });
            }

            await BuildEmbed()
                .WithTitle("Hiya, I'm Dexter~! Here's a list of modules and commands you can use!")
                .WithDescription("Use ~help [commandName] to show information about a command!")
                .WithFields(fields.ToArray())
                .SendEmbed(Context.Channel);
        }

        [Command("help")]
        [Summary("Displays detailed information about a command.")]
        public async Task HelpCommand(string Command) {
            SearchResult result = Service.Search(Context, Command);

            if (!result.IsSuccess) {
                await ReplyAsync("Sorry, I couldn't find a command like **" + Command + "**.");
                return;
            }

            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

            foreach (CommandMatch match in result.Commands) {
                CommandInfo cmd = match.Command;

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

            await BuildEmbed()
                .WithTitle("Here are some commands like **" + Command + "**")
                .WithFields(fields.ToArray())
                .SendEmbed(Context.Channel);
        }

        [Command("ping")]
        [Summary("Displays the latency between Dexter and Discord.")]
        public async Task PingCommand() {
            await BuildEmbed()
                .WithTitle("Gateway Ping")
                .WithDescription("**" + Context.Client.Latency + "ms**")
                .SendEmbed(Context.Channel);
        }

        [Command("uptime")]
        [Summary("Displays the amount of time Dexter has been running for!")]
        public async Task UptimeCommand() {
            TimeSpan uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            await BuildEmbed()
                .WithTitle("Uptime")
                .WithDescription("I've been runnin' for **" + uptime.Humanize() + "**! *yawns*")
                .SendEmbed(Context.Channel);
        }
    }
}

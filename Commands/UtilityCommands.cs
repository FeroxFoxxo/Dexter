using Dexter.Core;
using Dexter.Core.Enums;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public class UtilityCommands : AbstractModule {
        private readonly CommandService Service;

        public UtilityCommands(CommandService _Service) {
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

                    if (Description.Contains("~" + CommandInfo.Aliases.First()))
                        continue;

                    if (Result.IsSuccess)
                        Description.Add("~" + CommandInfo.Aliases.First());
                }

                if (Description.Count != 0)
                    Fields.Add(new EmbedFieldBuilder {
                        Name = Regex.Replace(Module.Name, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]),
                        Value = string.Join("\n", Description.ToArray())
                    });
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Hiya, I'm Dexter~! Here's a list of modules and commands you can use!")
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
                    .WithDescription("Sorry, I couldn't find a command like **" + Command + "**.")
                    .SendEmbed(Context.Channel);
                return;
            }

            List<EmbedFieldBuilder> Fields = new List<EmbedFieldBuilder>();

            foreach (CommandMatch Match in Result.Commands) {
                CommandInfo CommandInfo = Match.Command;

                string CommandDescription = "Parameters: " + string.Join(", ", CommandInfo.Parameters.Select(p => p.Name));

                if (CommandInfo.Parameters.Count > 0)
                    CommandDescription = "Parameters: " + string.Join(", ", CommandInfo.Parameters.Select(p => p.Name));
                else
                    CommandDescription = "No parameters";

                if (!string.IsNullOrEmpty(CommandInfo.Summary))
                    CommandDescription += "\nSummary: " + CommandInfo.Summary;

                Fields.Add(new EmbedFieldBuilder {
                    Name = string.Join(", ", CommandInfo.Aliases),
                    Value = CommandDescription,
                    IsInline = false
                });
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Here are some commands like **" + Command + "**!")
                .WithFields(Fields.ToArray())
                .SendEmbed(Context.Channel);
        }

        [Command("ping")]
        [Summary("Displays the latency between Dexter and Discord.")]
        public async Task PingCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Gateway Ping")
                .WithDescription("**" + Context.Client.Latency + "ms**")
                .SendEmbed(Context.Channel);
        }

        [Command("uptime")]
        [Summary("Displays the amount of time Dexter has been running for!")]
        public async Task UptimeCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Uptime")
                .WithDescription("I've been runnin' for **" + (DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize() + "**~!\n*yawns*")
                .SendEmbed(Context.Channel);
        }

        [Command("profile")]
        [Alias("userinfo")]
        [Summary("Gets the profile of the user mentioned or yours.")]
        public async Task ProfileCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle("User Profile For " + User.Username + "#" + User.Discriminator)
                .WithThumbnailUrl(User.GetAvatarUrl())
                .AddField("Username", User.Username)
                .AddField(!string.IsNullOrEmpty(User.Nickname), "Nickname", User.Nickname)
                .AddField("Created", User.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss") + " (" + User.CreatedAt.Humanize() + ")")
                .AddField(User.JoinedAt.HasValue, "Joined", User.JoinedAt?.ToString("dd/MM/yyyy HH:mm:ss") + " (" + User.JoinedAt.Humanize() + ")")
                .AddField("Status", User.Status)
                .SendEmbed(Context.Channel);
        }

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]
        public async Task AvatarCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await BuildEmbed(EmojiEnum.Unknown)
                .WithImageUrl(User.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithUrl(User.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithAuthor(User)
                .WithTitle("Get Avatar URL")
                .SendEmbed(Context.Channel);
        }

        [Command("emote")]
        [Alias("emoji")]
        [Summary("Gets the full image of an emote.")]
        public async Task EmojiCommand([Optional] string Emoji) {
            if (Emote.TryParse(Emoji, out var Emojis))
                await BuildEmbed(EmojiEnum.Unknown)
                    .WithImageUrl(Emojis.Url)
                    .WithUrl(Emojis.Url)
                    .WithAuthor(Emojis.Name)
                    .WithTitle("Get Emoji URL")
                    .SendEmbed(Context.Channel);
            else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Emoji")
                    .WithDescription("An invalid emote was specified! Please make sure that what you have sent is a valid emote. " +
                    "Please make sure this is a **custom emote** aswell and does not fall under the unicode specification.")
                    .SendEmbed(Context.Channel);
        }
    }
}

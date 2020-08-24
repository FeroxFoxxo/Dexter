using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Dexter.Core.Enums;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public class UtilityCommands : AbstractModule {
        public UtilityCommands(BotConfiguration _BotConfiguration) : base(_BotConfiguration) {}

        [Command("ping")]
        [Summary("Displays the latency between both Discord and I.")]
        public async Task PingCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Gateway Ping")
                .WithDescription($"**{Context.Client.Latency}ms**")
                .SendEmbed(Context.Channel);
        }

        [Command("uptime")]
        [Summary("Displays the amount of time I have been running for!")]
        public async Task UptimeCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Uptime")
                .WithDescription($"I've been runnin' for **{(DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize()}**~!\n*yawns*")
                .SendEmbed(Context.Channel);
        }

        [Command("profile")]
        [Alias("userinfo")]
        [Summary("Gets the profile of the user mentioned or yours.")]
        public async Task ProfileCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"User Profile For {User.Username}#{User.Discriminator}")
                .WithThumbnailUrl(User.GetAvatarUrl())
                .AddField("Username", User.Username)
                .AddField(!string.IsNullOrEmpty(User.Nickname), "Nickname", User.Nickname)
                .AddField("Created", $"{User.CreatedAt:dd/MM/yyyy HH:mm:ss} ({User.CreatedAt.Humanize()})")
                .AddField(User.JoinedAt.HasValue, "Joined", $"{(DateTimeOffset) User.JoinedAt:dd/MM/yyyy HH:mm:ss)} ({User.JoinedAt.Humanize()})")
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
                    .WithDescription("An invalid emote was specified! Please make sure that what you have sent is a valid emote. Please make sure this is a **custom emote** aswell and does not fall under the unicode specification.")
                    .SendEmbed(Context.Channel);
        }
    }
}

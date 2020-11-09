using Dexter.Enums;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Threading.Tasks;
using Dexter.Extensions;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("profile")]
        [Summary("Gets the profile of the user mentioned or yours.")]
        [Alias("userinfo")]

        public async Task ProfileCommand() {
            await ProfileCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("profile")]
        [Summary("Gets the profile of the user mentioned or yours.")]
        [Alias("userinfo")]

        public async Task ProfileCommand(IGuildUser User) {
            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"User Profile For {User.Username}#{User.Discriminator}")
                .WithThumbnailUrl(User.GetAvatarUrl())
                .AddField("Username", User.Username)
                .AddField(!string.IsNullOrEmpty(User.Nickname), "Nickname", User.Nickname)
                .AddField("Created", $"{User.CreatedAt:dd/MM/yyyy HH:mm:ss} ({User.CreatedAt.Humanize()})")
                .AddField(User.JoinedAt.HasValue, "Joined", $"{(DateTimeOffset)User.JoinedAt:dd/MM/yyyy HH:mm:ss)} ({User.JoinedAt.Humanize()})")
                .AddField("Status", User.Status)
                .SendEmbed(Context.Channel);
        }

    }
}

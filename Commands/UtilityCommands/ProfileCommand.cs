using Dexter.Core.Abstractions;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("profile")]
        [Summary("Gets the profile of the user mentioned or yours.")]
        [Alias("userinfo")]

        public async Task ProfileCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await Context.BuildEmbed(EmojiEnum.Unknown)
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

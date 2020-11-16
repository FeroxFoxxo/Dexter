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

        public async Task ProfileCommand(IGuildUser GuildUser) {
            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"User Profile For {GuildUser.Username}#{GuildUser.Discriminator}")
                .WithThumbnailUrl(GuildUser.GetAvatarUrl())
                .AddField("Username", GuildUser.Username)
                .AddField(!string.IsNullOrEmpty(GuildUser.Nickname), "Nickname", GuildUser.Nickname)
                .AddField("Created", $"{GuildUser.CreatedAt:dd/MM/yyyy HH:mm:ss} ({GuildUser.CreatedAt.Humanize()})")
                .AddField(GuildUser.JoinedAt.HasValue, "Joined", $"{(DateTimeOffset)GuildUser.JoinedAt:dd/MM/yyyy HH:mm:ss)} ({GuildUser.JoinedAt.Humanize()})")
                .AddField("ProposalStatus", GuildUser.Status)
                .SendEmbed(Context.Channel);
        }

    }
}

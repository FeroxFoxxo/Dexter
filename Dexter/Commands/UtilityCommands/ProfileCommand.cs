﻿using Dexter.Enums;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Threading.Tasks;
using Dexter.Extensions;
using System.Runtime.InteropServices;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        [Command("profile")]
        [Summary("Gets the profile of the user mentioned or yours.")]
        [Alias("userinfo")]

        public async Task ProfileCommand([Optional] IUser User) {
            IGuildUser GuildUser;

            if (User == null)
                GuildUser = Context.Guild.GetUser(Context.User.Id);
            else
                GuildUser = Context.Guild.GetUser(User.Id);

            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"User Profile For {GuildUser.Username}#{GuildUser.Discriminator}")
                .WithThumbnailUrl(GuildUser.GetTrueAvatarUrl())
                .AddField("Username", GuildUser.Username)
                .AddField(!string.IsNullOrEmpty(GuildUser.Nickname), "Nickname", GuildUser.Nickname)
                .AddField("Created", $"{GuildUser.CreatedAt:MM/dd/yyyy HH:mm:ss} ({GuildUser.CreatedAt.Humanize()})")
                .AddField(GuildUser.JoinedAt.HasValue, "Joined", !GuildUser.JoinedAt.HasValue ? string.Empty : $"{(DateTimeOffset)GuildUser.JoinedAt:MM/dd/yyyy HH:mm:ss} ({GuildUser.JoinedAt.Humanize()})")
                .AddField("User Status", GuildUser.Status.Humanize())
                .SendEmbed(Context.Channel);
        }

    }

}

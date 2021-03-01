using Dexter.Enums;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Threading.Tasks;
using Dexter.Extensions;
using System.Runtime.InteropServices;
using Dexter.Databases.Borkdays;
using Humanizer.Localisation;
using Discord.WebSocket;
using System.Linq;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Sends information concerning the profile of a target user.
        /// This information contains: Username, nickname, account creation and latest join date, and status.
        /// </summary>
        /// <param name="User">The target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("profile", RunMode = RunMode.Async)]
        [Summary("Gets the profile of the user mentioned or yours.")]
        [Alias("userinfo")]

        public async Task ProfileCommand([Optional] IUser User) {
            if (User == null)
                User = Context.User;

            IGuildUser GuildUser = await DiscordSocketClient.Rest.GetGuildUserAsync(Context.Guild.Id, User.Id);

            Borkday Borkday = BorkdayDB.Borkdays.Find(User.Id);

            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"User Profile For {GuildUser.Username}#{GuildUser.Discriminator}")
                .WithThumbnailUrl(GuildUser.GetTrueAvatarUrl())
                .AddField("Username", GuildUser.GetUserInformation())
                .AddField(!string.IsNullOrEmpty(GuildUser.Nickname), "Nickname", GuildUser.Nickname)
                .AddField("Created", $"{GuildUser.CreatedAt:MM/dd/yyyy HH:mm:ss} ({(DateTime.Now - GuildUser.CreatedAt.DateTime).Humanize(2, maxUnit: TimeUnit.Year)})")
                .AddField(GuildUser.JoinedAt.HasValue, "Joined", !GuildUser.JoinedAt.HasValue ? string.Empty : 
                    $"{(DateTimeOffset)GuildUser.JoinedAt:MM/dd/yyyy HH:mm:ss} ({(DateTime.Now - GuildUser.JoinedAt.Value.DateTime).Humanize(2, maxUnit: TimeUnit.Year)})")
                .AddField(Borkday != null, "Last Birthday", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Borkday != null ? Borkday.BorkdayTime : 0).ToLongDateString())
                .AddField("Top Role", Context.Guild.Roles.Where(Role => Role.Position == Context.Guild.GetUser(User.Id).Hierarchy).FirstOrDefault().Name)
                .SendEmbed(Context.Channel);
        }

    }

}

using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.UserProfiles;
using Dexter.Databases.EventTimers;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Gives a user the "Happy Borkday" role for 24 hours.
        /// </summary>
        /// <remarks>This command is staff-only.</remarks>
        /// <param name="User">The target user to give the role to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("borkday")]
        [Summary("Gives a user the borkday role for 24 hours! Happy birthday. <3")]
        [Alias("birthday")]
        [RequireModerator]

        public async Task GiveBorkday([Optional] IGuildUser User) {
            if (User == null)
                User = Context.Guild.GetUser(Context.User.Id);

            UserProfile Borkday = BorkdayDB.Profiles.Find(User.Id);

            if (Borkday == null)
                BorkdayDB.Profiles.Add(
                    new UserProfile() {
                        BorkdayTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        UserID = User.Id
                    }
                );
            else {
                if (Borkday.BorkdayTime + TimeSpan.FromDays(364).Seconds > DateTimeOffset.Now.ToUnixTimeSeconds()) {
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Unable To Give Borkday Role!")
                        .WithDescription($"Haiya! I was unable to give the borkday role as this user's last borkday was on " +
                            $"{new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Borkday.BorkdayTime).ToLongDateString()}."
                        )
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);

                    return;
                } else
                    Borkday.BorkdayTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            }

            BorkdayDB.SaveChanges();

            IRole Role = Context.Guild.GetRole(
                User.GetPermissionLevel(DiscordSocketClient, BotConfiguration) >= PermissionLevel.Moderator ?
                    ModerationConfiguration.StaffBorkdayRoleID : ModerationConfiguration.BorkdayRoleID
            );

            await User.AddRoleAsync(Role);

            await CreateEventTimer(
                RemoveBorkday,
                new() {
                    { "User", User.Id.ToString() },
                    { "Role", Role.Id.ToString() }
                },
                ModerationConfiguration.SecondsOfBorkday,
                TimerType.Expire
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Borkday role given!")
                .WithDescription($"Haiya! I have given {User.GetUserInformation()} the `{Role.Name}` role!\nWish them a good one <3")
                .WithCurrentTimestamp()
                .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,
                    BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Happy Borkday!")
                    .WithDescription($"Haiya! You have been given the {Role.Name} role on {Context.Guild.Name}. " +
                        $"Have a splendid birthday filled with lots of love and cheer!\n - {Context.Guild.Name} Staff <3")
                    .WithCurrentTimestamp()
                );
        }

        /// <summary>
        /// Removes the "Happy Borkday" role ahead of schedule if necessary.
        /// </summary>
        /// <param name="Parameters">The target user whose role is to be removed.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task RemoveBorkday(Dictionary<string, string> Parameters) {
            ulong UserID = Convert.ToUInt64(Parameters["User"]);
            ulong RoleID = Convert.ToUInt64(Parameters["Role"]);

            IGuild Guild = DiscordSocketClient.GetGuild(BotConfiguration.GuildID);

            IGuildUser User = await Guild.GetUserAsync(UserID);

            await User.RemoveRoleAsync(Guild.GetRole(RoleID));
        }

    }

}
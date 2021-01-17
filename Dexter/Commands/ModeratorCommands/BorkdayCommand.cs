using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
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
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        [Command("borkday")]
        [Summary("Gives a user the borkday role for 24 hours! Happy birthday. <3")]
        [Alias("birthday")]
        [RequireModerator]

        public async Task GiveBorkday ([Optional] IGuildUser User) {
            if (User == null)
                User = Context.Guild.GetUser(Context.User.Id);

            IRole Role = Context.Guild.GetRole(
                User.GetPermissionLevel(BotConfiguration) >= PermissionLevel.Moderator ?
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

            EmbedBuilder Embed = BuildEmbed(EmojiEnum.Love)
                .WithTitle("Borkday role given!")
                .WithDescription($"Haiya! I have given {User.GetUserInformation()} the `{Role.Name}` role!\nWish them a good one <3")
                .WithCurrentTimestamp();

            try {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Happy Borkday!")
                    .WithDescription($"Haiya! You have been given the {Role.Name} role on {Context.Guild.Name}. " +
                        $"Have a splendid birthday filled with lots of love and cheer!\n - {Context.Guild.Name} Staff <3")
                    .WithCurrentTimestamp()
                    .SendEmbed(await User.GetOrCreateDMChannelAsync());

                Embed.AddField("Success", "The DM was successfully sent!");
            } catch (HttpException) {
                Embed.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
            }

            await Embed.SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Removes the "Happy Borkday" role ahead of schedule if necessary.
        /// </summary>
        /// <param name="Parameters">The target user whose role is to be removed.</param>
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        public async Task RemoveBorkday(Dictionary<string, string> Parameters) {
            ulong UserID = Convert.ToUInt64(Parameters["User"]);
            ulong RoleID = Convert.ToUInt64(Parameters["Role"]);

            IGuild Guild = DiscordSocketClient.GetGuild(BotConfiguration.GuildID);

            IGuildUser User = await Guild.GetUserAsync(UserID);

            await User.RemoveRoleAsync(Guild.GetRole(RoleID));
        }

    }

}
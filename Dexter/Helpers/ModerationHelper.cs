using Dexter.Abstractions;
using Dexter.Commands;
using Dexter.Databases.EventTimers;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Discord;
using Humanizer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Helpers {

    /// <summary>
    /// Holds a variety of methods and utilities to help with moderation in a more straightforward manner
    /// </summary>

    public static class ModerationHelper
    {
        /// <summary>
        /// Issues a mute to a target <paramref name="User"/> for a duration of <paramref name="Time"/>; but doesn't save it to the user's records.
        /// </summary>
        /// <param name="User">The user to be muted.</param>
        /// <param name="Time">The duration of the mute.</param>
        /// <param name="ModeratorCommands">Context of Moderation commands required to manage mute infrastructure.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method finishes successfully.</returns>

        public static async Task MuteUser(this ModeratorCommands ModeratorCommands, IGuildUser User, TimeSpan Time)
        {
            DexterProfile DexterProfile = ModeratorCommands.InfractionsDB.GetOrCreateProfile(User.Id);

            if (ModeratorCommands.TimerService.TimerExists(DexterProfile.CurrentMute))
                ModeratorCommands.TimerService.RemoveTimer(DexterProfile.CurrentMute);

            IRole Role = User.Guild.GetRole(ModeratorCommands.ModerationConfiguration.MutedRoleID);

            try {
                if (!User.RoleIds.Contains(ModeratorCommands.ModerationConfiguration.MutedRoleID))
                    await User.AddRoleAsync(Role);
            }
            catch(Discord.Net.HttpException Error) {
                await (ModeratorCommands.DiscordSocketClient.GetChannel(ModeratorCommands.BotConfiguration.ModerationLogChannelID) as ITextChannel)
                    .SendMessageAsync($"**Missing Role Management Permissions >>>** <@&{ModeratorCommands.BotConfiguration.AdministratorRoleID}>",
                        embed: ModeratorCommands.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Error!")
                        .WithDescription($"Couldn't mute user <@{User.Id}> ({User.Id}) for {Time.Humanize()}.")
                        .AddField("Error:", Error.Message)
                        .WithCurrentTimestamp().Build()
                );
            }

            DexterProfile.CurrentMute = await DiscordModule.CreateEventTimer(ModeratorCommands.RemoveMutedRole, new() { { "UserID", User.Id.ToString() } }, Convert.ToInt32(Time.TotalSeconds), TimerType.Expire, ModeratorCommands.TimerService);
        }

    }
}

using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Databases.Levels;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// Manages the events and timers related to granting users Dexter experience for activity.
    /// </summary>

    public class LevelingService : Service {

        /// <summary>
        /// The relevant configuration related to the specific data and parameters of leveling.
        /// </summary>

        public LevelingConfiguration LevelingConfiguration { get; set; }

        /// <summary>
        /// A dedicated random number generator used for uniformly random XP determination.
        /// </summary>

        public Random Random { get; set; }

        /// <summary>
        /// The data structure holding all relevant information about user levels.
        /// </summary>

        public LevelingDB LevelingDB { get; set; }

        /// <summary>
        /// This method is run when the service is first started; which happens after dependency injection.
        /// </summary>

        public override async void Initialize() {
            EventTimer Timer = TimerService.EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.CallbackClass.Equals(GetType().Name)).FirstOrDefault();

            if (Timer != null)
                TimerService.EventTimersDB.EventTimers.Remove(Timer);

            DiscordSocketClient.MessageReceived += HandleMessage;

            await CreateEventTimer(AddLevels, new(), LevelingConfiguration.XPIncrementTime, TimerType.Interval);
        }

        /// <summary>
        /// Handles awarding XP to users based on voice activity and resetting timers on Text activity.
        /// </summary>
        /// <param name="parameters">Irrelevant argument used to fit event timer task form.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task AddLevels(Dictionary<string, string> parameters) {
            // Voice leveling up.

            IReadOnlyCollection<SocketVoiceChannel> vcs = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).VoiceChannels;

            foreach (SocketVoiceChannel voiceChannel in vcs) {
                int nonbotusers = 0;
                foreach(IGuildUser uservc in voiceChannel.Users)
                    if (!uservc.IsBot && !uservc.IsDeafened && !uservc.IsSelfDeafened) nonbotusers++;
                if (nonbotusers < LevelingConfiguration.VCMinUsers) continue;
                if (LevelingConfiguration.DisabledVCs.Contains(voiceChannel.Id)) continue;
                foreach (IGuildUser uservc in voiceChannel.Users)
                    if (!(uservc.IsMuted || uservc.IsDeafened || uservc.IsSelfMuted || uservc.IsSelfDeafened || uservc.IsBot)) {
                        await LevelingDB.IncrementUserXP(
                            Random.Next(LevelingConfiguration.VCMinXPGiven, LevelingConfiguration.VCMaxXPGiven),
                            false,
                            uservc,
                            DiscordSocketClient.GetChannel(LevelingConfiguration.VoiceTextChannel) as ITextChannel,
                            LevelingConfiguration.VoiceSendLevelUpMessage
                        );
                    }
            }

            foreach (UserTextXPRecord ur in LevelingDB.OnTextCooldowns) {
                LevelingDB.OnTextCooldowns.Remove(ur);
            }
            await LevelingDB.SaveChangesAsync();
        }

        private async Task HandleMessage(SocketMessage message) {
            if (!LevelingConfiguration.ManageTextXP) return;
            if (message.Author.IsBot) return;

            if (message.Channel is IDMChannel || LevelingConfiguration.DisabledTCs.Contains(message.Channel.Id)) return;

            if (LevelingDB.OnTextCooldowns.Find(message.Author.Id) is not null) return;

            await LevelingDB.IncrementUserXP(
                Random.Next(LevelingConfiguration.TextMinXPGiven, LevelingConfiguration.TextMaxXPGiven),
                true,
                message.Author as IGuildUser,
                message.Channel as ITextChannel,
                LevelingConfiguration.TextSendLevelUpMessage
                );

            LevelingDB.OnTextCooldowns.Add(new UserTextXPRecord() { Id = message.Author.Id });
            await LevelingDB.SaveChangesAsync();
        }

        /// <summary>
        /// Updates the ranked roles a user has based on their level.
        /// </summary>
        /// <param name="user">The user to modify the role list for.</param>
        /// <param name="removeExtra">Whether to remove roles above the rank of the user.</param>
        /// <param name="level">The level of the user, autocalculated if below 0.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task UpdateRoles(IGuildUser user, bool removeExtra = false, int level = -1) {
            if (user is null || !LevelingConfiguration.HandleRoles) return;

            if (level < 0) {
                UserLevel ul = LevelingDB.GetOrCreateLevelData(user.Id, out _);
                level = ul.TotalLevel(LevelingConfiguration);
            }

            List<IRole> toAdd = new();
            List<IRole> toRemove = new();

            foreach(KeyValuePair<int, ulong> rank in LevelingConfiguration.Levels) {

                if (level >= rank.Key && !user.RoleIds.Contains(rank.Value))
                    toAdd.Add(DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetRole(rank.Value));

                else if (removeExtra && level < rank.Key && user.RoleIds.Contains(rank.Value))
                    toRemove.Add(DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetRole(rank.Value));
            }

            if (toAdd.Count > 0)
                await user.AddRolesAsync(toAdd);
            if (toRemove.Count > 0)
                await user.RemoveRolesAsync(toRemove);
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Databases.Levels;
using Dexter.Databases.UserRestrictions;
using Discord;
using Discord.WebSocket;
using System.Text;

namespace Dexter.Services
{

    /// <summary>
    /// Manages the events and timers related to granting users Dexter experience for activity.
    /// </summary>

    public class LevelingService : Service
    {

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
        /// The data structure holding all relevant information for user restrictions.
        /// </summary>

        public RestrictionsDB RestrictionsDB { get; set; }

        /// <summary>
        /// Utility Configuration for private VCs.
        /// </summary>

        public UtilityConfiguration UtilityConfiguration {  get; set; }

        /// <summary>
        /// This method is run when the service is first started; which happens after dependency injection.
        /// </summary>

        public override async void Initialize()
        {
            EventTimer Timer = TimerService.EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.CallbackClass.Equals(GetType().Name)).FirstOrDefault();

            if (Timer != null)
                TimerService.EventTimersDB.EventTimers.Remove(Timer);

            DiscordShardedClient.MessageReceived += HandleMessage;
            DiscordShardedClient.UserJoined += HandleJoin;

            await CreateEventTimer(AddLevels, new(), LevelingConfiguration.XPIncrementTime, TimerType.Interval);
        }

        /// <summary>
        /// Handles awarding XP to users based on voice activity and resetting timers on Text activity.
        /// </summary>
        /// <param name="parameters">Irrelevant argument used to fit event timer task form.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task AddLevels(Dictionary<string, string> parameters)
        {
            // Voice leveling up.

            IReadOnlyCollection<SocketVoiceChannel> vcs = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).VoiceChannels;

            foreach (SocketVoiceChannel voiceChannel in vcs)
            {
                if (voiceChannel.CategoryId == UtilityConfiguration.PrivateCategoryID)
                    continue;

                int nonbotusers = 0;
                foreach (IGuildUser uservc in voiceChannel.Users)
                    if (!(uservc.IsBot 
                        || uservc.IsDeafened || uservc.IsSelfDeafened 
                        || RestrictionsDB.IsUserRestricted(uservc.Id, Restriction.VoiceXP)
                        || !LevelingConfiguration.VoiceCountMutedMembers && (uservc.IsMuted || uservc.IsSelfMuted || uservc.IsSuppressed))) 
                        nonbotusers++;
                if (nonbotusers < LevelingConfiguration.VCMinUsers) continue;
                if (LevelingConfiguration.DisabledVCs.Contains(voiceChannel.Id)) continue;
                foreach (IGuildUser uservc in voiceChannel.Users)
                    if (!(uservc.IsMuted || uservc.IsDeafened || uservc.IsSelfMuted || uservc.IsSelfDeafened || uservc.IsSuppressed 
                        || uservc.IsBot || RestrictionsDB.IsUserRestricted(uservc.Id, Restriction.VoiceXP)))
                    {
                        await LevelingDB.IncrementUserXP(
                            Random.Next(LevelingConfiguration.VCMinXPGiven, LevelingConfiguration.VCMaxXPGiven + 1),
                            false,
                            uservc,
                            DiscordShardedClient.GetChannel(LevelingConfiguration.VoiceTextChannel) as ITextChannel,
                            LevelingConfiguration.VoiceSendLevelUpMessage
                        );
                    }
            }

            LevelingDB.onTextCooldowns.RemoveWhere(e => true);
            LevelingDB.SaveChanges();
        }

        private async Task HandleMessage(SocketMessage message)
        {
            if (!LevelingConfiguration.ManageTextXP) return;
            if (message.Author.IsBot) return;

            if (message.Channel is IDMChannel || LevelingConfiguration.DisabledTCs.Contains(message.Channel.Id)) return;

            if (LevelingDB.onTextCooldowns.Contains(message.Author.Id)) return;

            await LevelingDB.IncrementUserXP(
                Random.Next(LevelingConfiguration.TextMinXPGiven, LevelingConfiguration.TextMaxXPGiven + 1),
                true,
                message.Author as IGuildUser,
                message.Channel as ITextChannel,
                LevelingConfiguration.TextSendLevelUpMessage
                );

            LevelingDB.onTextCooldowns.Add(message.Author.Id);
            LevelingDB.SaveChanges();

        }

        /// <summary>
        /// Updates the ranked roles a user has based on their level.
        /// </summary>
        /// <param name="user">The user to modify the role list for.</param>
        /// <param name="removeExtra">Whether to remove roles above the rank of the user.</param>
        /// <param name="level">The level of the user, autocalculated if below 0.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task<bool> UpdateRoles(IGuildUser user, bool removeExtra = false, int level = -1)
        {
            if (user is null || !LevelingConfiguration.HandleRoles) return false;

            if (level < 0)
            {
                UserLevel ul = LevelingDB.Levels.Find(user.Id);

                if (ul is null) return false;
                level = ul.TotalLevel(LevelingConfiguration);
            }

            List<IRole> toAdd = new();
            List<IRole> toRemove = new();

            SocketGuild guild = DiscordShardedClient.GetGuild(BotConfiguration.GuildID);
            HashSet<ulong> userRoles = user.RoleIds.ToHashSet();

            if (LevelingConfiguration.MemberRoleLevel > 0
                && level >= LevelingConfiguration.MemberRoleLevel
                && !userRoles.Contains(LevelingConfiguration.MemberRoleID))
            {
                toAdd.Add(guild.GetRole(LevelingConfiguration.MemberRoleID));
            }

            foreach (KeyValuePair<int, ulong> rank in LevelingConfiguration.Levels)
            {
                if (level >= rank.Key && !userRoles.Contains(rank.Value))
                    toAdd.Add(guild.GetRole(rank.Value));

                else if (removeExtra && level < rank.Key && userRoles.Contains(rank.Value))
                    toRemove.Add(guild.GetRole(rank.Value));
            }

            if (user.RoleIds.Contains(LevelingConfiguration.NicknameDisabledRole))
            {
                SocketRole replRole = guild.GetRole(LevelingConfiguration.NicknameDisabledReplacement);

                if (user.RoleIds.Contains(LevelingConfiguration.NicknameDisabledReplacement))
                    toRemove.Add(replRole);

                if (toAdd.Contains(replRole))
                    toAdd.Remove(replRole);
            }

            try
            {
                if (toAdd.Count > 0)
                    await user.AddRolesAsync(toAdd);
                if (toRemove.Count > 0)
                    await user.RemoveRolesAsync(toRemove);
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("At least one of the specified roles in configuration that should be applied does not exist!");
            }

            return toAdd.Count > 0 || toRemove.Count > 0;
        }

        /// <summary>
        /// Represents the result of a role modification operation.
        /// </summary>

        public class RoleModificationResponse
        {
            /// <summary>
            /// The target user of the role modification.
            /// </summary>
            public readonly IGuildUser target;
            /// <summary>
            /// Whether the role update was successful.
            /// </summary>
            public readonly bool success;
            /// <summary>
            /// The result of the operation; such as an error description or important extra information.
            /// </summary>
            public readonly string result;
            /// <summary>
            /// A dictionary containing the roles changed for the user. Items contained under <see langword="true"/> habe been added. Items contained under <see langword="false"/> have been removed. 
            /// </summary>
            public readonly Dictionary<bool, IEnumerable<IRole>> rolesChanged;
            /// <summary>
            /// The calculated level that the role modification is attempting to match.
            /// </summary>
            public readonly int readLevel;

            /// <summary>
            /// Creates a new response with the given parameters.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="success"></param>
            /// <param name="result"></param>
            /// <param name="rolesChanged"></param>
            /// <param name="readLevel"></param>

            public RoleModificationResponse(IGuildUser target, bool success, string result = "", Dictionary<bool, IEnumerable<IRole>> rolesChanged = null, int readLevel = -1)
            {
                this.target = target;
                this.success = success;
                this.result = result;
                if (rolesChanged is not null)
                    this.rolesChanged = rolesChanged;
                else
                    this.rolesChanged = new Dictionary<bool, IEnumerable<IRole>>() { { false, Array.Empty<IRole>() }, { true, Array.Empty<IRole>() } };
                this.readLevel = readLevel;

                this.readLevel = readLevel;
            }

            /// <summary>
            /// Gives a description of the response in a human-readable format.
            /// </summary>
            /// <returns>A string containing a human-readable description of the content of the object.</returns>
            public override string ToString()
            {
                if (success)
                {
                    List<string> removed = new();
                    List<string> added = new();
                    foreach (IRole r in rolesChanged[false]) removed.Add(r.Name);
                    foreach (IRole r in rolesChanged[true]) added.Add(r.Name);

                    StringBuilder txt = new();
                    txt.Append($"Changed roles for {target?.Username ?? "Unknown"} to match level {readLevel}.");
                    if (added.Count > 0) txt.Append($"\nAdded roles: {string.Join(", ", added)}.");
                    if (removed.Count > 0) txt.Append($"\nRemoved roles: {string.Join(", ", removed)}.");
                    if (!string.IsNullOrEmpty(result)) txt.Append($"\nAdditional info: {result}.");

                    return txt.ToString();
                }
                else
                {
                    return $"Changed no roles for {target?.Username ?? "Unknown"}; perceived level: {readLevel}. Result: {result}.";
                }
            }

            /// <summary>
            /// Logs the message using a logging service.
            /// </summary>
            /// <returns>An awaitable <see cref="Task"/> object.</returns>

            public async Task<RoleModificationResponse> Log()
            {
                return this;
            }
        }

        /// <summary>
        /// Updates the ranked roles a user has based on their level.
        /// </summary>
        /// <param name="user">The user to modify the role list for.</param>
        /// <param name="removeExtra">Whether to remove roles above the rank of the user.</param>
        /// <param name="level">The level of the user, autocalculated if below 0.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully; and yields a <see cref="RoleModificationResponse"/> with information about the completed operation.</returns>

        public async Task<RoleModificationResponse> UpdateRolesWithInfo(IGuildUser user, bool removeExtra = false, int level = -1)
        {
            if (user is null) return await new RoleModificationResponse(user, false, "Received user is null!").Log();
            if (!LevelingConfiguration.HandleRoles) return new RoleModificationResponse(user, false, "Dexter does not manage roles in this server!");

            if (level < 0)
            {
                UserLevel ul = LevelingDB.Levels.Find(user.Id);

                if (ul is null) return new RoleModificationResponse(user, false, "No level data to fetch! User has no leveling records.", readLevel: 0);
                level = ul.TotalLevel(LevelingConfiguration);
            }

            List<IRole> toAdd = new();
            List<IRole> toRemove = new();
            List<IRole> foundLeveledRoles = new();

            SocketGuild guild = DiscordShardedClient.GetGuild(BotConfiguration.GuildID);
            HashSet<ulong> userRoles = user.RoleIds.ToHashSet();

            if (LevelingConfiguration.MemberRoleLevel > 0
                && level >= LevelingConfiguration.MemberRoleLevel
                && !userRoles.Contains(LevelingConfiguration.MemberRoleID))
            {
                toAdd.Add(guild.GetRole(LevelingConfiguration.MemberRoleID));
            }

            foreach (KeyValuePair<int, ulong> rank in LevelingConfiguration.Levels)
            {
                if (level >= rank.Key && !userRoles.Contains(rank.Value))
                    toAdd.Add(guild.GetRole(rank.Value));

                else if (userRoles.Contains(rank.Value))
                {
                    IRole r = guild.GetRole(rank.Value);
                    foundLeveledRoles.Add(r);
                    if (removeExtra && level < rank.Key)
                        toRemove.Add(r);
                }
            }

            if (user.RoleIds.Contains(LevelingConfiguration.NicknameDisabledRole))
            {
                SocketRole replRole = guild.GetRole(LevelingConfiguration.NicknameDisabledReplacement);

                if (user.RoleIds.Contains(LevelingConfiguration.NicknameDisabledReplacement))
                    toRemove.Add(replRole);

                if (toAdd.Contains(replRole))
                    toAdd.Remove(replRole);
            }

            try
            {
                if (toAdd.Count > 0)
                    await user.AddRolesAsync(toAdd);
                if (toRemove.Count > 0)
                    await user.RemoveRolesAsync(toRemove);
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("At least one of the specified roles in configuration that should be applied does not exist!");
            }

            bool success = toAdd.Count > 0 || toRemove.Count > 0;
            List<string> rolesFoundNames = new();
            foreach (IRole r in foundLeveledRoles)
            {
                rolesFoundNames.Add(r.Name);
            }
            string rolesFoundExpression = rolesFoundNames.Count == 0 ? "" : $"Found roles: [{string.Join(", ", rolesFoundNames)}]";
            string message = (success ? "" : "No role modifications are necessary; updated no roles. ") + rolesFoundExpression;
            Dictionary<bool, IEnumerable<IRole>> mods = new() { { false, toRemove }, { true, toAdd } };
            return new RoleModificationResponse(user, success, message, mods, level);
        }

        /// <summary>
        /// Detects when a user joins the guild and immediately assigns them their ranked roles.
        /// </summary>
        /// <param name="user">The user that joined the guild.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task HandleJoin(SocketGuildUser user)
        {
            await UpdateRoles(user);
        }

    }

}

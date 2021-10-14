using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Services;
using Discord;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dexter.Databases.Levels
{

    /// <summary>
    /// An abstraction of the data structure holding all relevant information about user levels.
    /// </summary>

    public class LevelingDB : Database
    {

        /// <summary>
        /// The configuration file that contains all relevant data for the leveling module.
        /// </summary>

        public LevelingConfiguration LevelingConfiguration { get; set; }

        /// <summary>
        /// Generic bot configuration that relates to overarching settings.
        /// </summary>

        public BotConfiguration BotConfiguration { get; set; }

        /// <summary>
        /// The data structure containing information for user XP.
        /// </summary>

        public DbSet<UserLevel> Levels { get; set; }

        /// <summary>
        /// Stores a collection of user preferences for rank card display.
        /// </summary>

        public DbSet<LevelPreferences> Prefs { get; set; }

        /// <summary>
        /// The data structure containing all instances of users on Text XP cooldowns.
        /// </summary>

        public HashSet<ulong> onTextCooldowns = new();

        /// <summary>
        /// Gets a level entry from the database or creates one if none exist for <paramref name="id"/>
        /// </summary>
        /// <param name="id">The ID of the user to fetch.</param>
        /// <param name="save">Whether to save the dabase if the user needs to be created.</param>
        /// <returns>A <see cref="UserLevel"/> object that corresponds to the given <paramref name="id"/> and is being tracked by the database context.</returns>

        public UserLevel GetOrCreateLevelData(ulong id, bool save = true)
        {
            UserLevel level = Levels.Find(id);

            if (level is null)
            {
                level = new UserLevel
                {
                    UserID = id,
                    TextXP = 0,
                    VoiceXP = 0
                };

                Levels.Add(level);
                if (save)
                    SaveChanges();
            }

            return level;
        }

        /// <summary>
        /// Gets a level record for a given user, or creates one if it doesn't exist.
        /// </summary>
        /// <param name="id">The ID of the user to look up.</param>
        /// <param name="settings">The corresponding User Preferences object for this user.</param>
        /// <param name="save">Whether to save the data to the database if a user or preferences object needs to be created.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns>A <see cref="UserLevel"/> object containing the obtained XP levels for the given <paramref name="id"/>.</returns>

        public UserLevel GetOrCreateLevelData(ulong id, out LevelPreferences settings, bool save = true)
        {
            UserLevel level;
            try
            {
                level = Levels.Find(id);
                settings = Prefs.Find(id);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new InvalidOperationException($"An database error occurred while trying to find a level and preferences item for ID {id}:" +
                    $"\n{e}");
            }

            bool toSave = false;
            if (level is null)
            {
                level = new UserLevel
                {
                    UserID = id,
                    TextXP = 0,
                    VoiceXP = 0
                };

                Levels.Add(level);
                toSave = true;
            }

            if (settings is null)
            {
                settings = new()
                {
                    UserId = id
                };

                Prefs.Add(settings);
                toSave = true;
            }

            if (toSave && save) SaveChanges();

            return level;
        }

        /// <summary>
        /// The service that manages leveling internally.
        /// </summary>

        [NotMapped]
        public LevelingService LevelingService { get; set; }

        /// <summary>
        /// Grants a given user an amount of XP and announces the level up in <paramref name="fallbackChannel"/> if appropriate.
        /// </summary>
        /// <param name="xpIncrease">The amount of XP to grant the user.</param>
        /// <param name="isTextXp">Whether to grant Text XP or Voice XP.</param>
        /// <param name="user">Which user to grant the XP to.</param>
        /// <param name="fallbackChannel">The channel to send the level up message in, if appropriate.</param>
        /// <param name="sendLevelUp">Whether to send a level up message at all.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task IncrementUserXP(int xpIncrease, bool isTextXp, IGuildUser user, ITextChannel fallbackChannel, bool sendLevelUp)
        {
            UserLevel userlevel = Levels.Find(user.Id);

            if (userlevel == null)
            {
                userlevel = new UserLevel() { UserID = user.Id, TextXP = 0, VoiceXP = 0 };
                Levels.Add(userlevel);
                await SaveChangesAsync();
            }

            int currentLevel;
            int otherLevel;
            long xp;

            if (isTextXp)
            {
                userlevel.TextXP += xpIncrease;
                currentLevel = LevelingConfiguration.GetLevelFromXP(userlevel.TextXP, out xp, out _);
            }
            else
            {
                userlevel.VoiceXP += xpIncrease;
                currentLevel = LevelingConfiguration.GetLevelFromXP(userlevel.VoiceXP, out xp, out _);
            }

            int newLevel = 0;
            bool tryMerge = LevelingConfiguration.LevelMergeMode is LevelMergeMode.AddXPSimple or LevelMergeMode.AddXPMerged;
            bool mergeLevelUp = false;
            if (tryMerge)
            {
                long maxXP = userlevel.TextXP > userlevel.VoiceXP ? userlevel.TextXP : userlevel.VoiceXP;
                long minXP = userlevel.TextXP > userlevel.VoiceXP ? userlevel.VoiceXP : userlevel.TextXP;
                newLevel = LevelingConfiguration.GetLevelFromXP(maxXP
                    + (long)(minXP * (LevelingConfiguration.LevelMergeMode == LevelMergeMode.AddXPMerged ? LevelingConfiguration.MergeFactor : 1)), out long resXP, out _);
                mergeLevelUp = resXP < xpIncrease;
            }

            if ((xp < xpIncrease && !tryMerge) || mergeLevelUp)
            {
                if (!tryMerge)
                {
                    if (isTextXp)
                    {
                        otherLevel = LevelingConfiguration.GetLevelFromXP(userlevel.VoiceXP, out _, out _);
                    }
                    else
                    {
                        otherLevel = LevelingConfiguration.GetLevelFromXP(userlevel.TextXP, out _, out _);
                    }
                    newLevel = userlevel.TotalLevel(LevelingConfiguration, isTextXp ? currentLevel : otherLevel, isTextXp ? otherLevel : currentLevel);
                }
                if (sendLevelUp)
                    await fallbackChannel.SendMessageAsync(LevelingConfiguration.LevelUpMessage
                        .Replace("{TYPE}", mergeLevelUp ? "total" : isTextXp ? "text" : "voice")
                        .Replace("{MENTION}", user.Mention)
                        .Replace("{LVL}", (mergeLevelUp ? newLevel : currentLevel).ToString()));

                await LevelingService.UpdateRoles(user, false, mergeLevelUp ? newLevel : currentLevel);

                if (LevelingConfiguration.MemberRoleLevel > 0
                    && !user.RoleIds.Contains(LevelingConfiguration.MemberRoleID)
                    && LevelingConfiguration.HandleRoles)
                {
                    IRole memrole = user.Guild.GetRole(LevelingConfiguration.MemberRoleID);

                    await user.AddRoleAsync(memrole);
                }
            }

            await SaveChangesAsync();
        }

    }

}

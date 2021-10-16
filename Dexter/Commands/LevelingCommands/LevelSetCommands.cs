using System;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Sets a user's XP of a given type to a given value.
        /// </summary>
        /// <param name="user">The user to modify the XP for.</param>
        /// <param name="xptype">The type of XP to set, either text or voice.</param>
        /// <param name="amount">The value to set the XP to.</param>
        /// <param name="unit">Whether to set the xp or the level</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes succesfully.</returns>

        [Command("setxp")]
        [Summary("Modifies a user's XP. Usage: `setxp [User] [Type] [Amount] [Units]`. Types are \"text\", \"voice\". Units are \"xp\", \"lvl\".")]
        [RequireAdministrator]

        public async Task SetXPCommand(IUser user, string xptype, long amount, string unit = "xp")
        {
            UserLevel ul = LevelingDB.GetOrCreateLevelData(user.Id, out _);
            bool isTextXP;
            switch (xptype.ToLower())
            {
                case "text":
                case "tc":
                case "txt":
                    isTextXP = true;
                    break;
                case "voice":
                case "vc":
                    isTextXP = false;
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to resolve XP type")
                        .WithDescription($"Received \"{xptype}\", expecting either \"text\" or \"voice\".")
                        .SendEmbed(Context.Channel);
                    return;
            }

            int newLvl;
            switch (unit.ToLower())
            {
                case "x":
                case "xp":
                case "exp":
                    long prevXP;
                    if (isTextXP)
                    {
                        prevXP = ul.TextXP;
                        ul.TextXP = amount;
                    }
                    else
                    {
                        prevXP = ul.VoiceXP;
                        ul.VoiceXP = amount;
                    }
                    newLvl = LevelingConfiguration.GetLevelFromXP(amount, out _, out _);

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Successful Operation!")
                        .WithDescription($"XP for user {user.Mention} has been changed from {prevXP} to {amount} (level {newLvl}).")
                        .SendEmbed(Context.Channel);
                    break;
                case "l":
                case "lv":
                case "lvl":
                case "level":
                case "levels":
                    if (amount > int.MaxValue)
                    {
                        amount = int.MaxValue;
                    }
                    int prevLvl;
                    newLvl = (int)amount;
                    try
                    {
                        if (isTextXP)
                        {
                            prevLvl = LevelingConfiguration.GetLevelFromXP(ul.TextXP, out _, out _);
                            ul.TextXP = checked(LevelingConfiguration.GetXPForLevel(newLvl));
                        }
                        else
                        {
                            prevLvl = LevelingConfiguration.GetLevelFromXP(ul.VoiceXP, out _, out _);
                            ul.VoiceXP = checked(LevelingConfiguration.GetXPForLevel(newLvl));
                        }
                    }
                    catch (OverflowException)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Overflow Exception")
                            .WithDescription("The value provided causes user XP to exceed its maximum possible value!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Successful Operation!")
                        .WithDescription($"XP for user {user.Mention} has been changed from level {prevLvl} to the beginning of level {newLvl}.")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unrecognized Unit")
                        .WithDescription($"Unable to parse \"{unit}\" into a valid unit. Please use \"xp\" or \"lvl\".")
                        .SendEmbed(Context.Channel);
                    return;
            }

            await LevelingService.UpdateRoles(DiscordShardedClient.GetGuild(BotConfiguration.GuildID)?.GetUser(user.Id), true);
        }

        /// <summary>
        /// Grants a given user an amount of experience of the chosen type.
        /// </summary>
        /// <param name="user">The target user.</param>
        /// <param name="xptype">The type of xp to grant, either text or voice.</param>
        /// <param name="amount">The amount of xp to grant the user.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("givexp")]
        [Alias("grantxp", "addxp")]
        [Summary("Grants a user a given amount of experience. Usage: `givexp [User] [Type] [Amount]`. Types are \"text\", \"voice\"")]
        [RequireAdministrator]

        public async Task GiveXPCommand(IUser user, string xptype, long amount)
        {
            UserLevel ul = LevelingDB.GetOrCreateLevelData(user.Id, out _);
            bool isTextXP;
            switch (xptype.ToLower())
            {
                case "text":
                case "tc":
                case "txt":
                    isTextXP = true;
                    break;
                case "voice":
                case "vc":
                    isTextXP = false;
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to resolve XP type")
                        .WithDescription($"Received \"{xptype}\", expecting either \"text\" or \"voice\".")
                        .SendEmbed(Context.Channel);
                    return;
            }

            try
            {
                if (isTextXP)
                {
                    ul.TextXP = checked(ul.TextXP + amount);
                }
                else
                {
                    ul.VoiceXP = checked(ul.VoiceXP + amount);
                }
            }
            catch (OverflowException)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Overflow Exception")
                            .WithDescription("The value provided causes user XP to exceed its maximum possible value!")
                            .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Successful Operation!")
                .WithDescription($"The {(isTextXP ? "text" : "voice")} XP for user {user.Mention} has been {(amount >= 0 ? "increased" : "decreased")} by {Math.Abs(amount)}!")
                .SendEmbed(Context.Channel);

            await LevelingService.UpdateRoles(DiscordShardedClient.GetGuild(BotConfiguration.GuildID)?.GetUser(user.Id), true);
        }
    }
}

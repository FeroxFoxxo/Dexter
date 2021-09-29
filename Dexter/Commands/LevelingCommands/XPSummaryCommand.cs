using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using System.Text.RegularExpressions;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Presents a report of information about a user's experience.
        /// </summary>
        /// <param name="arguments">A list of space-separated arguments that modify the behaviour of the command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("xpsummary")]
        [Alias("xp", "xpinfo")]
        [Summary("Displays useful leveling information. Use arguments to display specific information; such as `user:[UserID]`")]
        [ExtendedSummary("Displays useful leveling information. You can use special arguments of the format `[attribute]:[value]` to fine-tune the results you want to obtain.\n" +
            "`user:[userID]` will select a user to report about.\n" +
            "`level:[level]` will select the target level to calculate average times for.\n" +
            "`rank:[rank role name]` will select the target ranked for average time calculation.")]
        [BotChannel]

        public async Task XPSummaryCommand([Remainder] string arguments = "")
        {
            string[] args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            ulong targetID = Context.User.Id;
            UserLevel ul;

            int targetlvl = 0;
            bool postcalcTargetLevel = true;


            int targetRankLevel = 0;
            string targetRankName = "";
            bool postcalcTargetRank = true;

            List<string> errors = new();

            foreach (string arg in args)
            {
                int colonIndex = arg.IndexOf(':');
                if (colonIndex < 0)
                {
                    errors.Add($"Unable to recognize argument {arg}; missing a colon (:)");
                    continue;
                }

                string attr = arg[..colonIndex];
                string value = arg[(colonIndex + 1)..];

                switch (attr.ToLower())
                {
                    case "user":
                        string idstr = Regex.Match(value, @"[0-9]{18}").Value;
                        if (string.IsNullOrEmpty(idstr))
                        {
                            errors.Add($"Unable to recognize user {value}; the argument contains no correctly formatted ID.");
                            continue;
                        }
                        targetID = ulong.Parse(idstr);
                        break;
                    case "level":
                        if (!int.TryParse(value, out targetlvl))
                        {
                            errors.Add($"Unable to parse level {value} to a whole number.");
                            continue;
                        }
                        postcalcTargetLevel = false;
                        break;
                    case "rank":
                    case "role":
                        IRole result = null;
                        List<string> roleNames = new();
                        foreach (KeyValuePair<int, ulong> levelRole in LevelingConfiguration.Levels)
                        {
                            SocketRole r = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetRole(levelRole.Value);
                            if (r.Name.ToLower().Replace(" ", "") == value.ToLower()
                                || r.Name.ToLower().Replace(' ', '_') == value.ToLower())
                            {
                                result = r;
                                targetRankLevel = levelRole.Key;
                                targetRankName = r.Name;
                                postcalcTargetRank = false;
                                break;
                            }
                            else
                            {
                                roleNames.Add(r.Name.Replace(' ', '_'));
                            }
                        }
                        if (result == null)
                        {
                            errors.Add($"Unable to parse ranked role {value}; please use one of the following: \"{string.Join("\", \"", roleNames)}\"");
                            continue;
                        }
                        break;
                    default:
                        errors.Add($"Unable to parse attribute name: \"{attr}\"");
                        continue;
                }
            }

            ul = LevelingDB.Levels.Find(targetID);
            if (ul is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User not found in database.")
                    .WithDescription($"We weren't able to find any logged user with the ID {targetID}")
                    .SendEmbed(Context.Channel);
                return;
            }

            int textLevel = LevelingConfiguration.GetLevelFromXP(ul.TextXP, out long resTextXP, out long levelTextXP);
            int voiceLevel = LevelingConfiguration.GetLevelFromXP(ul.VoiceXP, out long resVoiceXP, out long levelVoiceXP);
            long totalXP = ul.TotalXP(LevelingConfiguration);
            int totalLevel = LevelingConfiguration.GetLevelFromXP(totalXP, out long resTotalXP, out long levelTotalXP);

            if (postcalcTargetLevel)
            {
                targetlvl = ul.TotalLevel(LevelingConfiguration, textLevel, voiceLevel) + 1;
            }
            long targetxp = LevelingConfiguration.GetXPForLevel(targetlvl);

            if (postcalcTargetRank)
            {
                bool found = false;
                int maxLevel = 0;
                foreach (KeyValuePair<int, ulong> levelRole in LevelingConfiguration.Levels)
                {
                    SocketRole r = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetRole(levelRole.Value);
                    if (levelRole.Key > totalLevel)
                    {
                        targetRankLevel = levelRole.Key;
                        targetRankName = r.Name;
                        found = true;
                        break;
                    }
                    else if (levelRole.Key > maxLevel) maxLevel = levelRole.Key;
                }

                if (!found)
                {
                    targetRankLevel = maxLevel;
                    targetRankName = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetRole(LevelingConfiguration.Levels[maxLevel]).Name;
                }
            }
            long targetrankxp = LevelingConfiguration.GetXPForLevel(targetRankLevel);

            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle($"XP Summary for: {targetID}")
                .WithDescription($"Level {totalLevel} ({resTotalXP}/{levelTotalXP})\n" +
                $"Text Level: {textLevel} ({resTextXP}/{levelTextXP})\n" +
                $"Voice Level: {voiceLevel} ({resVoiceXP}/{levelVoiceXP})")
                .AddField("Experience", $"{totalXP} ({ul.TextXP} from text + {ul.VoiceXP} from voice)")
                .AddField($"Till level {targetlvl}:", LevelTargetExpression(totalXP, targetxp))
                .AddField($"Till rank {targetRankName}:", LevelTargetExpression(totalXP, targetrankxp))
                .AddField(errors.Any(), "Errors:", string.Join("; ", errors))
                .SendEmbed(Context.Channel);
        }

        private string LevelTargetExpression(long currentXP, long targetXP)
        {
            string textExpr;
            try
            {
                TimeSpan textTime = TimeSpan.FromMinutes((targetXP - currentXP) / ((LevelingConfiguration.TextMinXPGiven + LevelingConfiguration.TextMaxXPGiven) >> 1));
                textExpr = textTime.Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Minute);
            }
            catch
            {
                textExpr = "a **very** long time";
            }
            string voiceExpr;
            try
            {
                TimeSpan voiceTime = TimeSpan.FromMinutes((targetXP - currentXP) / ((LevelingConfiguration.VCMinXPGiven + LevelingConfiguration.VCMaxXPGiven) >> 1));
                voiceExpr = voiceTime.Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Minute);
            }
            catch
            {
                voiceExpr = "a **very** long time";
            }
            if (targetXP > currentXP) return $"{currentXP} out of {targetXP} experience. Missing {targetXP - currentXP}; " +
                $"which can be obtained in an average of {textExpr} through text activity " +
                $"or in an average of {voiceExpr} through voice activity.";
            else return $"{currentXP} out of {targetXP}. Exceeded target by {currentXP - targetXP} experience.";
        }

    }
}

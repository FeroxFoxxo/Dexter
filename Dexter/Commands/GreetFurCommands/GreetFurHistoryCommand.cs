using Dexter.Databases.GreetFur;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    public partial class GreetFurCommands
    {
        /// <summary>
        /// Displays an embed with general information obtained from the GreetFur records system.
        /// </summary>
        /// <param name="user">Target user to query record entries for.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        [Command("gfhistory")]
        [Summary("Shows a record of GreetFur activity for a specific user.")]
        [Alias("greetfurhistory", "gfrecords", "greetfurrecords")]

        public async Task GreetFurHistoryCommand(IUser user = null)
        {
            user ??= Context.User;

            List<GreetFurRecord> records = [.. GreetFurDB.Records.AsQueryable().Where(r => r.UserId == user.Id)];

            records.Sort((a, b) => a.Date.CompareTo(b.Date));

            if (records.Count == 0)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("No Records!")
                    .WithDescription("Unable to pull data from the database for the selected user. No records exist.")
                    .SendEmbed(Context.Channel);
                return;
            }

            TimeZoneData tz = ProfilesDB.GetOrCreateProfile(user.Id).GetRelevantTimeZone(LanguageConfiguration);
            DateTimeOffset firstRecord = DateTimeOffset.FromUnixTimeSeconds(records.First().Date * (60 * 60 * 24)).ToOffset(tz.TimeOffset);

            int total = 0;
            int maxStreak = 0;
            int exemptions = 0;
            int daysGuess = 0;

            int streak = 0;
            int lastDay = records.First().Date - 1;

            foreach (GreetFurRecord r in records)
            {
                int diff = r.Date - lastDay;
                lastDay = r.Date;
                if (diff != 1)
                {
                    maxStreak = streak > maxStreak ? streak : maxStreak;
                    streak = 0;
                }

                bool yes = (r.MutedUser && GreetFurConfiguration.GreetFurActiveWithMute) || r.MessageCount >= GreetFurConfiguration.GreetFurMinimumDailyMessages;
                if (yes)
                {
                    streak++;
                    total++;
                }
                else if (!r.IsExempt)
                {
                    maxStreak = streak > maxStreak ? streak : maxStreak;
                    streak = 0;
                }
                else
                {
                    exemptions++;
                }

                if (r.IsExempt)
                {
                    daysGuess++;
                }
                else
                {
                    daysGuess += diff;
                }
            }
            maxStreak = streak > maxStreak ? streak : maxStreak;


            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Record for {user.Username}")
                .WithDescription($"Overall performance: {total} / {daysGuess - exemptions} ({100 * (float)total / (daysGuess - exemptions):G3}%)")
                .AddField("Yes", total, true)
                .AddField("No", daysGuess - exemptions - total, true)
                .AddField("Exemptions", exemptions, true)
                .AddField("Longest Streak", maxStreak)
                .AddField("Started On", $"{firstRecord: MMMM d, yyyy}")
                .AddField("Current Day", $"{DateTimeOffset.Now.ToOffset(tz.TimeOffset): dddd MMMM d, yyyy (hh:mm tt)} {tz.Name}")
                .SendEmbed(Context.Channel);
        }
    }
}

using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.GreetFur;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Services;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dexter.Helpers.LanguageHelper;

namespace Dexter.Commands
{

    public partial class GreetFurCommands
    {
        const string SUMMARY = "Sets variables for a GreetFur's activity in the records for a given period of time.";
        const string HELPSUMMARY = "";

        /// <summary>
        /// Alters a GreetFur's activity based on input arguments.
        /// </summary>
        /// <param name="user">The GreetFur to alter the activity for.</param>
        /// <param name="arguments">Set of time periods to select followed by activity expressions.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        [Command("setgreetfuractivity")]
        [Summary(SUMMARY)]
        [ExtendedSummary(HELPSUMMARY)]
        [Alias("setgfactivity", "setactivity")]
        [RequireModerator]
        [BotChannel]

        public async Task SetGreetFurActivityCommand(IUser user, [Remainder] string arguments)
        {
            await SetGreetFurActivityCommand(user.Id, arguments);
        }

        /// <summary>
        /// Alters a GreetFur's activity based on input arguments.
        /// </summary>
        /// <param name="id">The ID of the GreetFur to alter the activity for.</param>
        /// <param name="arguments">Set of time periods to select followed by activity expressions.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        [Command("setgreetfuractivity")]
        [Summary(SUMMARY)]
        [ExtendedSummary(HELPSUMMARY)]
        [Alias("setgfactivity", "setactivity")]
        [RequireModerator]
        [BotChannel]

        public async Task SetGreetFurActivityCommand(ulong id, [Remainder] string arguments)
        {
            string[] args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            bool success = false;

            List<string> feedback = new();
            for (int i = 1; i < args.Length; i += 2)
            {
                bool tsuccess = GreetFurTimePeriod.TryParse(args[i - 1], out GreetFurTimePeriod t, out string tfb, GreetFurConfiguration);
                bool asuccess = TryParseGreetFurActivity(args[i], out GreetFurActivityTemplate a, out string afb);

                success |= tsuccess && asuccess;
                if (tsuccess && asuccess)
                {
                    ApplyRecords(id, t, a);
                    feedback.Add($"{tfb} - {afb}");
                }
                else if (tsuccess)
                    feedback.Add($"ERROR: Invalid activity expression - **{afb}**");
                else if (asuccess)
                    feedback.Add($"ERROR: Invalid time expression - **{tfb}**");
                else
                    feedback.Add($"ERROR: Invalid time and activity expressions - **{tfb}** and **{afb}**");
            }

            await BuildEmbed(success ? EmojiEnum.Love : EmojiEnum.Annoyed)
                .WithTitle(success ? "GreetFur activity modifications" : "ERROR:")
                .WithDescription(string.Join('\n', feedback))
                .SendEmbed(Context.Channel);
        }

        static readonly Dictionary<string, Weekday> strToWeekday = new() {
            { "mo", Weekday.Monday },
            { "tu", Weekday.Tuesday },
            { "we", Weekday.Wednesday },
            { "th", Weekday.Thursday },
            { "fr", Weekday.Friday },
            { "sa", Weekday.Saturday },
            { "su", Weekday.Sunday }
        };

        private bool TryParseGreetFurActivity(string input, out GreetFurActivityTemplate activity, out string feedback)
        {
            activity = new();
            for (int i = 0; i < input.Length; i++)
            {
                switch(input[i])
                {

                    case 'e':
                    case 'E':
                        activity.flags |= ActivityFlags.Exempt;
                        break;
                    case 'f':
                    case 'F':
                        activity.force = true;
                        break;
                    case 'm':
                    case 'M':
                        activity.flags |= ActivityFlags.MutedUser;
                        break;
                    default:
                        string remainder = input[i..];
                        if (!int.TryParse(remainder, out int msgCount))
                        {
                            feedback = $"Invalid token found for message count expression \"{remainder}\"; part of activity expression \"{input}\"\n" +
                                $"An activity expression must be composed of at least a set of flags ('E', 'F' or 'M') and/or a number of messages; with no whitespace in the expression.";
                            return false;
                        }
                        activity.messageCount = msgCount;
                        break;
                }
            }

            List<string> flagsExpr = new();
            if (activity.flags.HasFlag(ActivityFlags.Exempt)) flagsExpr.Add("Exempt");
            if (activity.flags.HasFlag(ActivityFlags.MutedUser)) flagsExpr.Add("Mute");
            if (activity.force) flagsExpr.Add("Force");
            feedback = $"{activity.messageCount} messages"
                + (flagsExpr.Count > 0 ? $" with flags: {flagsExpr.Enumerate()}." : ".");

            return true;
        }

        internal class GreetFurActivityTemplate
        {
            public ActivityFlags flags;
            public bool force;
            public int messageCount;

            public void Apply(GreetFurRecord record)
            {
                if (force)
                {
                    record.Activity = flags;
                    record.MessageCount = messageCount;
                } 
                else
                {
                    record.Activity |= flags;
                    record.MessageCount = record.MessageCount > messageCount ? record.MessageCount : messageCount;
                }
            }
        }

        private void ApplyRecords(ulong userId, GreetFurTimePeriod time, GreetFurActivityTemplate template)
        {
            for (int d = time.start; d <= time.end; d++)
            {
                GreetFurRecord record = GreetFurDB.AddActivity(userId, 0, ActivityFlags.None, d, false);
                template.Apply(record);
            }
            GreetFurDB.SaveChanges();
        }

        internal class GreetFurTimePeriod
        {
            public int start;
            public int end;

            public GreetFurTimePeriod(int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public static bool TryParse(string input, out GreetFurTimePeriod result, out string feedback, GreetFurConfiguration config)
            {
                result = null;

                string[] segments = input.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 2)
                {
                    feedback = "Excessive amount of segments provided, the time expression may only contain one hyphen (-) and it must separate two time expressions.";
                    return false;
                }

                List<int> days = new(2);
                foreach(string s in segments)
                {
                    Match m;
                    m = Regex.Match(s, @"w([0-9]{1,11})(mo|tu|we|th|fr|sa|su)[a-z]*", RegexOptions.IgnoreCase);

                    if (m.Success)
                    {
                        days.Add(config.FirstTrackingDay + (int.Parse(m.Groups[1].Value) * 7 - 7) 
                            + (int)strToWeekday[m.Groups[2].Value.ToLower()]);
                        continue;
                    }

                    m = Regex.Match(s, @"([0-9]{4}/)?([0-9]{1,2})/([0-9]{1,2})");

                    if (m.Success)
                    {
                        int year = DateTimeOffset.Now.Year;
                        if (m.Groups[1].Success)
                        {
                            year = int.Parse(m.Groups[1].Value[..^1]);
                        }

                        int month = int.Parse(m.Groups[2].Value);
                        int day = int.Parse(m.Groups[3].Value);

                        if (year <= 1970)
                        {
                            feedback = $"Time set to an excessively old value! Year selected: {year}";
                            return false;
                        }

                        try
                        {
                            DateTimeOffset newDay = new(year, month, day, 0, 0, 0, TimeSpan.Zero);
                            days.Add(Databases.GreetFur.GreetFurDB.GetDayFromDate(newDay));
                            continue;
                        }
                        catch (Exception e)
                        {
                            feedback = $"{e.GetType()}: {e.Message}. Use a valid time; received year = {year}, month = {month}, day = {day}.";
                            return false;
                        }

                    }

                    feedback = $"Invalid day format! \"{s}\". Follow either the w[week][weekday] format or the (yyyy/)mm/dd format.";
                    return false;
                }

                result = new GreetFurTimePeriod(days[0], days.Last());

                if (result.end < result.start)
                {
                    int temp = result.start;
                    result.start = result.end;
                    result.end = temp;
                }

                if (days.Count == 1)
                {
                    int wd = (result.start - config.FirstTrackingDay) % 7;
                    int w = (result.start - config.FirstTrackingDay) / 7;
                    feedback = $"{(Weekday)wd} of week {w + 1}";
                }
                else
                {
                    int wds = (result.start - config.FirstTrackingDay) % 7;
                    int ws = (result.start - config.FirstTrackingDay) / 7;

                    int wde = (result.end - config.FirstTrackingDay) % 7;
                    int we = (result.end - config.FirstTrackingDay) / 7;

                    feedback = $"{(Weekday)wds} of week {ws + 1} to {(Weekday)wde} of week {we + 1}";
                }
                return true;
            }
        }
    }

}

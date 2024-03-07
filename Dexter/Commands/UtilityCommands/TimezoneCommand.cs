using Dexter.Attributes;
using Dexter.Databases.UserProfiles;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord.Commands;
using Humanizer;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    public partial class UtilityCommands
    {

        /// <summary>
        /// Gives general information about time zones, or compares them, or searches for one, depending on <paramref name="Action"/>.
        /// </summary>
        /// <param name="Action">The action to take respective to other arguments. Leave empty for general information.</param>
        /// <param name="argument">Everything that comes after the <paramref name="Action"/> when the command is run, generally a date or time zone.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("timezone")]
        [Summary("Gives information about time zones or searches for a specific one.")]
        [ExtendedSummary("Gives information about time zones or searches for a specific one.\n" +
            "`timezone SEARCH [TERM]` - Looks for similar timezone names and gives information about each one.\n" +
            "`timezone SPAN [TIMEZONE]` - Looks for time zones with similar or exactly the same offsets as given.\n" +
            "`timezone GET [ABBREVIATION]` - Gets information about one specific time zone.\n" +
            "`timezone WHEN (DATE) [TIME] (ZONE)` - Gets the time difference between now and the specified date & time in the given time zone.\n" +
            "`timezone DIFF [ZONE1] [ZONE2]` - Compares two time zones\n" +
            "`timezone NOW [TIMEZONE]` - Gets the current time in a given time zone.\n" +
            "`timezone USER [USER]` - Gets the current time and time zone of a user from the social system.")]
        [BotChannel]

        public async Task TimezoneCommand(string Action = "", [Remainder] string argument = "")
        {

            if (string.IsNullOrEmpty(Action) || Action.Equals("info", StringComparison.CurrentCultureIgnoreCase))
            {
                await BuildEmbed(EmojiEnum.Sign)
                    .WithTitle("Time Zone Info")
                    .WithDescription("Time Zones are used to coordinate the times you input with those of other members in different parts of the world.\n" +
                        "Many countries use a mechanism called **Daylight Saving Time** (DST), whereby time gets advanced for 1 hour during the summer half of the year.\n" +
                        "When inputting your time zone, make sure to check whether your local area uses DST or not, and specify the correct time zone.\n" +
                        $"Dexter is running in {TimeZoneData.ToTimeZoneExpression(DateTimeOffset.Now.Offset)}.")

                    .SendEmbed(Context.Channel);
                return;
            }

            TimeZoneData timeZone;
            switch (Action.ToLower())
            {
                case "search":
                    if (string.IsNullOrEmpty(argument))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments!")
                            .WithDescription("You must provide a Time Zone Abbreviation to search for!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    {
                        string[] results = LanguageHelper.SearchTimeZone(argument, LanguageConfiguration);
                        string[] resultsHumanized = new string[Math.Min(10, results.Length)];

                        for (int i = 0; i < resultsHumanized.Length; i++)
                        {
                            resultsHumanized[i] = $"{results[i]}: {LanguageConfiguration.TimeZones[results[i]]}";
                        }

                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Top 10 Results")
                            .WithDescription(string.Join("\n", resultsHumanized))
                            .SendEmbed(Context.Channel);
                    }
                    return;
                case "span":
                    if (string.IsNullOrEmpty(argument))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments!")
                            .WithDescription("You must provide a Time Zone Expression to search for!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    {
                        if (!TimeZoneData.TryParse(argument, LanguageConfiguration, out TimeZoneData TimeZone))
                        {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Couldn't find time zone!")
                                .WithDescription($"Time Zone {argument} doesn't exist. Use `{BotConfiguration.Prefix}timezone search {argument}` to look for similar ones.")
                                .SendEmbed(Context.Channel);
                            return;
                        }

                        string[] results = LanguageHelper.SearchTimeZone(TimeZone.TimeOffset, LanguageConfiguration, out int Exact);
                        string[] resultsHumanized = new string[Math.Min(Math.Max(Exact, 10), results.Length)];

                        for (int i = 0; i < resultsHumanized.Length; i++)
                        {
                            resultsHumanized[i] = $"{results[i]}: {LanguageConfiguration.TimeZones[results[i]]}";
                        }

                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle($"Top {resultsHumanized.Length} Results similar to {TimeZoneData.ToTimeZoneExpression(TimeZone.Offset)}")
                            .WithDescription(string.Join("\n", resultsHumanized))
                            .SendEmbed(Context.Channel);
                    }
                    return;
                case "now":
                    if (string.IsNullOrEmpty(argument))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments!")
                            .WithDescription("You must provide a Time Zone Expression to search for!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    {
                        if (!TimeZoneData.TryParse(argument, LanguageConfiguration, out timeZone))
                        {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Couldn't find time zone!")
                                .WithDescription($"Time Zone {argument} doesn't exist. Use `{BotConfiguration.Prefix}timezone search {argument}` to look for similar ones.")
                                .SendEmbed(Context.Channel);
                            return;
                        }

                        await Context.Channel.SendMessageAsync($"It is currently **{DateTimeOffset.Now.ToOffset(timeZone.TimeOffset):dddd',' MMMM d',' hh:mm tt}** in {TimeZoneData.ToTimeZoneExpression(timeZone.Offset)} ({timeZone.Name}).");
                    }
                    return;
                case "get":
                    if (string.IsNullOrEmpty(argument))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Invalid number of arguments!")
                                .WithDescription("You must provide a Time Zone Abbreviation to search for!")
                                .SendEmbed(Context.Channel);
                        return;
                    }

                    if (!LanguageConfiguration.TimeZones.TryGetValue(argument, out TimeZoneData value))
                    {
                        string[] results = LanguageHelper.SearchTimeZone(argument, LanguageConfiguration);
                        string[] resultsHumanized = new string[Math.Min(3, results.Length)];

                        for (int i = 0; i < resultsHumanized.Length; i++)
                        {
                            resultsHumanized[i] = $"{results[i]}: {LanguageConfiguration.TimeZones[results[i]]}";
                        }

                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to find time zone!")
                            .WithDescription($"Did you mean...\n{string.Join("\n", resultsHumanized)}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Found time zone!")
                        .WithDescription($"{argument}: {value}")
                        .SendEmbed(Context.Channel);
                    return;
                case "when":
                case "until":
                case "till":
                    if (string.IsNullOrEmpty(argument))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Invalid number of arguments!")
                                .WithDescription("You must provide a Date and Time to compare to!")
                                .SendEmbed(Context.Channel);
                        return;
                    }

                    if (!LanguageHelper.TryParseTime(argument, CultureInfo.CurrentCulture, LanguageConfiguration, out DateTimeOffset time, out _))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Failed to parse date!")
                            .WithDescription($"I was unable to parse the time: `{argument}`\n Make sure it follows the correct format! For more info, check out `{BotConfiguration.Prefix}checktime [Your Date]`")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Found Time!")
                        .WithDescription($"{time:MMM dd, yyyy hh:mm tt 'UTC'zzz} {(time.CompareTo(DateTimeOffset.Now) < 0 ? "happened" : "will happen")} {time.Humanize()}.")
                        .SendEmbed(Context.Channel);

                    return;
                case "diff":
                case "difference":
                case "comp":
                case "compare":
                    if (string.IsNullOrEmpty(argument))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Invalid number of arguments!")
                                .WithDescription("You must provide two time zones to compare!")
                                .SendEmbed(Context.Channel);
                        return;
                    }

                    {
                        string[] args = argument.Split(" ");
                        if (args.Length < 2)
                        {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Invalid number of arguments!")
                                .WithDescription("You haven't provided enough time zones to compare! You must provide two.")
                                .SendEmbed(Context.Channel);
                            return;
                        }

                        TimeZoneData[] timeZones = new TimeZoneData[2];

                        for (int i = 0; i < timeZones.Length; i++)
                        {
                            if (!TimeZoneData.TryParse(args[i], LanguageConfiguration, out timeZones[i]))
                            {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Couldn't find time zone!")
                                    .WithDescription($"Time Zone {args[i]} doesn't exist. Use `{BotConfiguration.Prefix}timezone search {args[i]}` to look for similar ones.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }
                        }

                        float diff = timeZones[0].Offset - timeZones[1].Offset;

                        string message;
                        if (diff != 0)
                        {
                            message = $"{args[0]} ({timeZones[0].Name}) is " +
                                 $"**{LanguageHelper.HumanizeSexagesimalUnits(Math.Abs(diff), ["hour", "hours"], ["minute", "minutes"], out _)} " +
                                 $"{(diff < 0 ? "behind" : "ahead of")}** {args[1]} ({timeZones[1].Name}).";
                        }
                        else
                        {
                            message = $"{args[0]} ({timeZones[0].Name}) is **in sync with** {args[1]} ({timeZones[1].Name}).";
                        }

                        await Context.Channel.SendMessageAsync(message);
                    }
                    return;
                case "user":
                    string idStr = Regex.Match(argument, @"[0-9]{18}").Value;

                    if (string.IsNullOrEmpty(idStr))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Argument can't be parsed to a user!")
                            .WithDescription("Please include a user you'd like to know the time for.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    ulong userID = ulong.Parse(idStr);
                    UserProfile profile = ProfilesDB.Profiles.Find(userID);

                    if (profile is null)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("User doesn't have a profile!")
                            .WithDescription("No data for this user to obtain a time zone from.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    timeZone = profile.GetRelevantTimeZone(LanguageConfiguration);
                    TimeZoneData.TryParse("UTC+0:00", LanguageConfiguration, out TimeZoneData defRef);
                    if (timeZone.Name == defRef.Name)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("No custom time zone!")
                            .WithDescription("This user hasn't configured their personal time zone in their profile :(")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    DateTimeOffset timeNow = DateTimeOffset.Now.ToOffset(timeZone.TimeOffset);
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{timeNow:hh:mm tt '-' MMM dd}")
                        .WithDescription($"<@{userID}>'s time is: {timeNow:MMM dd';' hh:mm tt} {timeZone.Name}.")
                        .SendEmbed(Context.Channel);
                    return;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unrecognized action!")
                        .WithDescription($"Unable to parse action \"`{Action}`\"! \nFor more information on accepted actions, check out the `help timezone` command.")
                        .SendEmbed(Context.Channel);
                    return;
            }
        }
    }
}

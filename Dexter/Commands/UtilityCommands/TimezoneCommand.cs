using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        /// <summary>
        /// Gives general information about time zones, or compares them, or searches for one, depending on <paramref name="Action"/>.
        /// </summary>
        /// <param name="Action">The action to take respective to other arguments. Leave empty for general information.</param>
        /// <param name="FirstArg"></param>
        /// <param name="RestArgs"></param>
        /// <returns></returns>

        [Command("timezone")]
        [Summary("Gives information about time zones or searches for a specific one.")]
        [ExtendedSummary("Gives information about time zones or searches for a specific one.\n" +
            "`timezone SEARCH [TERM]` - Looks for similar timezone names and gives information about each one.\n" +
            "`timezone GET [ABBREVIATION]` - Gets information about one specific time zone")]
        [BotChannel]

        public async Task TimezoneCommand(string Action = "", string FirstArg = "", [Remainder] string RestArgs = "") {
            
            if(string.IsNullOrEmpty(Action) || Action.ToLower() == "info") {
                await BuildEmbed(EmojiEnum.Sign)
                    .WithTitle("Time Zone Info")
                    .WithDescription("Time Zones are used to coordinate the times you input with those of other members in different parts of the world.\n" +
                        "Many countries use a mechanism called **Daylight Saving Time** (DST), whereby time gets advanced for 1 hour during the summer half of the year.\n" +
                        "When inputting your time zone, make sure to check whether your local area uses DST or not, and specify the correct time zone.\n" +
                        $"Dexter is running in {TimeZoneData.ToTimeZoneExpression(DateTimeOffset.Now.Offset)}.")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
                    return;
            }

            switch(Action.ToLower()) {
                case "search":
                    if(string.IsNullOrEmpty(FirstArg)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments!")
                            .WithDescription("You must provide a Time Zone Abbreviation to search for!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string[] Results = LanguageHelper.SearchTimeZone(FirstArg, LanguageConfiguration);
                    string[] ResultsHumanized = new string[Math.Min(10, Results.Length)];

                    for(int i = 0; i < ResultsHumanized.Length; i++)
                        ResultsHumanized[i] = $"{Results[i]}: {LanguageConfiguration.TimeZones[Results[i]]}";

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Top 10 Results")
                        .WithDescription(string.Join("\n", ResultsHumanized))
                        .SendEmbed(Context.Channel);
                    return;
                case "get":
                    if (string.IsNullOrEmpty(FirstArg)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Invalid number of arguments!")
                                .WithDescription("You must provide a Time Zone Abbreviation to search for!")
                                .SendEmbed(Context.Channel);
                        return;
                    }
                    
                    if(!LanguageConfiguration.TimeZones.ContainsKey(FirstArg)) {
                        string[] ProbableSubstitutes = LanguageHelper.SearchTimeZone(FirstArg, LanguageConfiguration);
                        string[] ProbableSubstitutesHumanized = new string[Math.Min(3, ProbableSubstitutes.Length)];

                        for (int i = 0; i < ProbableSubstitutesHumanized.Length; i++)
                            ProbableSubstitutesHumanized[i] = $"{ProbableSubstitutes[i]}: {LanguageConfiguration.TimeZones[ProbableSubstitutes[i]]}";

                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to find time zone!")
                            .WithDescription($"Did you mean...\n{string.Join("\n", ProbableSubstitutesHumanized)}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Found time zone!")
                        .WithDescription($"{FirstArg}: {LanguageConfiguration.TimeZones[FirstArg]}")
                        .SendEmbed(Context.Channel);
                    return;
            }
        }
    }
}

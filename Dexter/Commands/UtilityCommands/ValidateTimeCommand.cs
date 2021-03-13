using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord.Commands;
using Humanizer;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Dexter.Commands {
    partial class UtilityCommands {

        /// <summary>
        /// Provides help and support to the users to help them use proper syntax in terms of inputting correct DateTimeOffset-parsable strings.
        /// </summary>
        /// <param name="InputDate">The input to try to parse into a DateTimeOffset, if any.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("checktime")]
        [Summary("Validates date and time formats to input into other time-sensitive commands.\n" +
            "Use the command without arguments to get general information or input a time after it to check whether it can be parsed correctly!" +
            "Use `checktime examples` to see a few examples.")]
        [BotChannel]

        public async Task CheckTimeCommand([Remainder] string InputDate = "") {
            if(string.IsNullOrEmpty(InputDate)) {

                await Context.Channel.SendMessageAsync("Dates are given in the following format. Elements in parentheses are optional.\n" +
                        $"**(DATE)**: {LanguageHelper.DEFAULT_DATE_FORMAT_INFO} **|** Month = full name or 3 first letters of a month, dd = day of month, year = full year or last two numbers.\n" +
                        $"**TIME**: {LanguageHelper.DEFAULT_TIME_FORMAT_INFO} **|** Hours, minutes, seconds, and 12h-system specifier if any; otherwise it is interpreted as 24h.\n" +
                        $"**OFFSET**: {LanguageHelper.DEFAULT_OFFSET_FORMAT_INFO} **|** Time zone, expressed either as an abbreviation (see `{BotConfiguration.Prefix}timezone search [Abbr]`) or as an offset from UTC.\n" +
                        $"For examples, use `{BotConfiguration.Prefix}checktime examples`. For more information check the `{BotConfiguration.Prefix}timezone` command");
                return;
            }

            if(InputDate.ToLower() == "examples") {
                await BuildEmbed(EmojiEnum.Sign)
                    .WithTitle("Date Format Examples")
                    .WithDescription(
                        "Nov 13, 2022 11:00 pm EDT\n" +
                        "November 13 23:00 EDT\n" +
                        "13 nov 2022 11pm -4\n" +
                        "7:00 UTC-4\n" +
                        "1 march 7 AM +11:30\n" +
                        "1 Mar 7:00 GMT+11:30\n" +
                        "3/1 7:00:45 am Z+11:30\n" +
                        "3/1/22 7:00:45.1234 GST3"
                    ).SendEmbed(Context.Channel);
                return;
            }

            if(!InputDate.TryParseTime(CultureInfo.CurrentCulture, LanguageConfiguration, out DateTimeOffset Time, out string Error)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Parse attempt failed!")
                    .WithDescription(Error)
                    .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Success!")
                .WithDescription($"The time you input parses to: {Time:MMM d',' yyyy 'at' hh:mm:ss tt 'UTC'zzz} ({Time.Humanize()}).")
                .SendEmbed(Context.Channel);
        }

    }
}

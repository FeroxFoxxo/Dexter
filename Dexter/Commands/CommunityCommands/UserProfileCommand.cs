using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Databases.UserProfiles;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Humanizer;

namespace Dexter.Commands {
    
    public partial class CommunityCommands {

        /// <summary>
        /// Edits or queries data from a user's personal user profile.
        /// </summary>
        /// <param name="action">The action to take, either get or set.</param>
        /// <param name="parameters">In case of setting an attribute, the attribute to change followed by the value to set it to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("me")]
        [Alias("myprofile")]

        public async Task MyProfileCommand(string action = "get", [Remainder] string parameters = "") {

            UserProfile profile = ProfilesDB.GetOrCreateProfile(Context.User.Id);

            switch(action.ToLower()) {
                case "get":
                case "profile":
                    bool TZSuccess = LanguageHelper.TryParseTimeZone(profile.TimeZone, LanguageConfiguration, out TimeZoneData TZ);
                    bool TZDSTSuccess = LanguageHelper.TryParseTimeZone(profile.TimeZoneDST, LanguageConfiguration, out TimeZoneData TZDST);

                    await new EmbedBuilder()
                        .WithColor(Color.DarkPurple)
                        .WithThumbnailUrl(Context.User.GetTrueAvatarUrl())
                        .WithTitle($"{((Context.User as IGuildUser)?.Nickname ?? Context.User.Username).Possessive()} Profile")
                        .WithDescription($"Custom user profile for user: {Context.User.GetUserInformation()}")
                        .AddField(!string.IsNullOrEmpty(profile.Gender), "Gender", profile.Gender, true)
                        .AddField(!string.IsNullOrEmpty(profile.Orientation), "Sexuality", profile.Orientation, true)
                        .AddField(profile.Borkday != default, "Borkday", $"{profile.Borkday}" + (profile.BirthYear > 0 ? $", {profile.BirthYear} (**Age**: {GetAge(profile, out _)})" : ""))
                        .AddField(TZSuccess, "Time Zone", TZ.ToString() + ((profile.TimeZone != profile.TimeZoneDST && TZDSTSuccess) ? $" ({TZDST} during Daylight Savings. [{profile.DSTRules?.ToString() ?? "Never"}])" : ""))
                        .AddField(!string.IsNullOrEmpty(profile.Nationality), "Nationality", profile.Nationality, true)
                        .AddField(!string.IsNullOrEmpty(profile.Languages), "Languages", profile.Languages, true)
                        .AddField(!string.IsNullOrEmpty(profile.SonaInfo), "Sona", profile.SonaInfo)
                        .AddField(!string.IsNullOrEmpty(profile.Info), "Info", profile.Info)
                        .SendEmbed(Context.Channel);
                    break;
                case "set":
                case "edit":
                    int separatorIndex = parameters.IndexOf(' ');
                    if (separatorIndex < 0) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid Number of Parameters")
                            .WithDescription("You must provide at least an attribute name to change and a new value for it.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    string attribute = parameters[..separatorIndex];
                    string value = parameters[separatorIndex..].Trim();

                    if (value.Length > CommunityConfiguration.MaxProfileAttributeLength) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Excessively long value")
                            .WithDescription($"Keep your profile fields under a maximum length of {CommunityConfiguration.MaxProfileAttributeLength}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string feedback = "";
                    switch(attribute.ToLower()) {
                        case "timezone":
                        case "tz":
                            if (!LanguageHelper.TryParseTimeZone(value, LanguageConfiguration, out TimeZoneData timeZone)) {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to find time zone")
                                    .WithDescription($"Couldn't find time zone {value}. Make sure you capitalize it correctly. You can use the `~timezone search [abbreviation]` command to look for similar recognized time zones.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            profile.TimeZone = value;
                            profile.TimeZoneDST = value;
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Successfully set default time zone!")
                                .WithDescription($"Your default and DST time zones have been set to {timeZone}.")
                                .SendEmbed(Context.Channel);

                            await CreateBirthdayTimer(profile);
                            break;
                        case "dst":
                        case "dsttz":
                        case "dsttimezone":
                        case "tzdst":
                        case "timezonedst":
                            if (value == "none") {
                                profile.TimeZoneDST = "";
                                await GenericResetInfo("DST Time Zone");
                                ProfilesDB.SaveChanges();
                                return;
                            }

                            if (!LanguageHelper.TryParseTimeZone(value, LanguageConfiguration, out TimeZoneData timeZoneDST)) {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to find time zone")
                                    .WithDescription($"Couldn't find time zone {value}. Make sure you capitalize it correctly. You can use the `~timezone search [abbreviation]` command to look for similar recognized time zones.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            profile.TimeZoneDST = value;
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Successfully set DST time zone")
                                .WithDescription($"Your DST-specific time zone has been set to {timeZoneDST}.")
                                .SendEmbed(Context.Channel);

                            await CreateBirthdayTimer(profile);
                            break;
                        case "dstrules":
                        case "dstrule":
                            if (value == "none") {
                                profile.DSTRules = null;
                                await BuildEmbed(EmojiEnum.Sign)
                                    .WithTitle("Cleared data about DST application rules")
                                    .WithDescription($"All previous information about DST patterns has been removed. From now on, only your default time zone will be considered for any calculations.")
                                    .SendEmbed(Context.Channel);
                                ProfilesDB.SaveChanges();
                                return;
                            }

                            if (!DaylightShiftRules.TryParse(value, LanguageConfiguration, out feedback, out DaylightShiftRules rules)) {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to parse time zone rules")
                                    .WithDescription(feedback)
                                    .AddField("Format Info:", "Absolute: [N]th of [Month] to [N]th of [Month]. Relative: [N]th [Weekday] of [Month] to [N]th [Weekday] of [Month].")
                                    .AddField("Examples:", "24th of April to 8th of October; 1st Sunday of March to 2nd Sunday of September; last Sunday of Oct to first Monday of Feb.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            profile.DSTRules = rules;
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Successfully set Local DST Rules")
                                .WithDescription(feedback)
                                .SendEmbed(Context.Channel);

                            await CreateBirthdayTimer(profile);
                            break;
                        case "borkday":
                        case "birthday":
                            if (value.ToLower() == "none") {
                                profile.Borkday = default;
                                TryRemoveBirthdayTimer(profile);
                                ProfilesDB.SaveChanges();
                                await BuildEmbed(EmojiEnum.Sign)
                                    .WithTitle("Removed local birthday records.")
                                    .WithDescription("Your birth day is no longer being tracked by the profile system.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            if (!DayInYear.TryParse(value, true, LanguageConfiguration, out DayInYear day, out feedback, out int year)) {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to parse birthday.")
                                    .WithDescription(feedback)
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            profile.Borkday = day;
                            bool yearChanged = year < DateTime.Now.Year - 1;
                            if (yearChanged) { profile.BirthYear = year; }

                            await CreateBirthdayTimer(profile);

                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Successfully set Birthday")
                                .WithDescription($"Your birthday has been set to the {day}{(yearChanged ? $", {year}" : "")}")
                                .SendEmbed(Context.Channel);
                            break;
                        case "birthyear":
                            if (value.ToLower() == "none") {
                                profile.BirthYear = default;
                                await BuildEmbed(EmojiEnum.Sign)
                                    .WithTitle("Removed local birth year records")
                                    .WithDescription("Your birth year is no longer being tracked by the profile system.")
                                    .SendEmbed(Context.Channel);
                                ProfilesDB.SaveChanges();
                                return;
                            }

                            if(!int.TryParse(value, out int result)) {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to parse value into a number")
                                    .WithDescription("Please enter a decimal value, entering 0 is equivalent to clearing the birth year.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }
                            profile.BirthYear = result;
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Successfully set birth year")
                                .WithDescription($"Your birth year has been updated to the value: {result}")
                                .SendEmbed(Context.Channel);
                            break;
                        case "gender":
                        case "pronouns":
                            if (value == "none") {
                                profile.Gender = "";
                                await GenericResetInfo("Gender");
                            }
                            else {
                                profile.Gender = value;
                                await GenericSuccessInfo("Gender", value);
                            }
                            break;
                        case "sexuality":
                        case "orientation":
                            if (value == "none") {
                                profile.Orientation = "";
                                await GenericResetInfo("Sexuality");
                            }
                            else {
                                profile.Orientation = value;
                                await GenericSuccessInfo("Sexuality", value);
                            }
                            break;
                        case "sona":
                        case "fursona":
                        case "sonainfo":
                            if (value == "none") {
                                profile.SonaInfo = "";
                                await GenericResetInfo("Sona Info");
                            }
                            else {
                                profile.SonaInfo = value;
                                await GenericSuccessInfo("Sona Info", value);
                            }
                            break;
                        case "nation":
                        case "country":
                        case "nationality":
                            if (value == "none") {
                                profile.Nationality = "";
                                await GenericResetInfo("Nationality");
                            }
                            else {
                                profile.Nationality = value;
                                await GenericSuccessInfo("Nationality", value);
                            }
                            break;
                        case "languages":
                        case "language":
                            if (value == "none") {
                                profile.Languages = "";
                                await GenericResetInfo("Languages");
                            }
                            else {
                                profile.Languages = value;
                                await GenericSuccessInfo("Languages", value);
                            }
                            break;
                        case "about":
                        case "information":
                        case "info":
                        case "data":
                        case "miscellaneous":
                        case "misc":
                        case "other":
                            if (value == "none") {
                                profile.Info = "";
                                await GenericResetInfo("Info");
                            }
                            else {
                                profile.Info = value;
                                await GenericSuccessInfo("Info", value);
                            }
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unknown Attribute")
                                .WithDescription($"Unknown attribute: \"{attribute}\". Currently valid attributes are: Gender, Sexuality, Timezone, TimeZoneDST, DSTRules, Birthday, Birthyear, Gender, Sexuality, Nation, Languages and Miscellaneous.")
                                .SendEmbed(Context.Channel);
                            break;
                    }
                    ProfilesDB.SaveChanges();
                    return;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Invalid Action")
                        .WithDescription($"Unrecognized action: {action}, please use either \"get\" or \"set\".")
                        .SendEmbed(Context.Channel);
                    return;
            }

        }

        private async Task GenericSuccessInfo(string attribute, object value) {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Successfully changed value of \"{attribute}\"")
                .WithDescription($"New value set to: {value}")
                .SendEmbed(Context.Channel);
        }

        private async Task GenericResetInfo(string attribute) {
            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle($"Cleared local data about \"{attribute}\".")
                .WithDescription("The data you've previously introduced for this field has been removed and is no longer being tracked.")
                .SendEmbed(Context.Channel);
        }

        private string GetAge(UserProfile profile, out TimeSpan age) {
            age = default;
            
            if (profile.BirthYear <= 0) {
                return "Insufficient information; Missing birth year.";
            }
            else {
                if (profile.Borkday != default) {
                    age = DateTimeOffset.Now.Subtract(GetBirthDay(profile));
                    return $"{age.Humanize(3, maxUnit: Humanizer.Localisation.TimeUnit.Year, minUnit: Humanizer.Localisation.TimeUnit.Day)}";
                }
                int yrs = DateTime.Now.Year - profile.BirthYear;
                age = TimeSpan.FromDays(yrs * 365.24);
                return $"Approximately {yrs} years.";
            }
        }

        private DateTimeOffset GetNow(UserProfile profile) {
            return DateTimeOffset.Now.ToOffset(GetRelevantTimeZone(profile).TimeOffset);
        }

        private DateTimeOffset GetBirthDay(UserProfile profile) {
            int day = profile.Borkday?.Day ?? 0;
            LanguageHelper.Month month = profile.Borkday?.Month ?? LanguageHelper.Month.January;
            return new DateTimeOffset(new DateTime(profile.BirthYear <= 0 ? DateTime.Now.Year : profile.BirthYear, (int)month, day, 0, 0, 0), GetRelevantTimeZone(profile)?.TimeOffset ?? default);
        }

        private TimeZoneData GetRelevantTimeZone(UserProfile profile) {
            return GetRelevantTimeZone(profile, DateTimeOffset.Now);
        }

        private TimeZoneData GetRelevantTimeZone(UserProfile profile, DateTimeOffset day) {
            if (!LanguageHelper.TryParseTimeZone(profile.TimeZone, LanguageConfiguration, out TimeZoneData TZ))
                return null;

            if (profile.DSTRules is null || !profile.DSTRules.IsDST(day)) {
                return TZ;
            }
            else {
                if (!LanguageHelper.TryParseTimeZone(profile.TimeZoneDST, LanguageConfiguration, out TimeZoneData TZDST)) {
                    return TZ;
                }
                return TZDST;
            }
        }

        private bool TryRemoveBirthdayTimer(UserProfile profile) {
            if (!string.IsNullOrEmpty(profile.BorkdayTimerToken) && TimerService.TimerExists(profile.BorkdayTimerToken)) {
                TimerService.RemoveTimer(profile.BorkdayTimerToken);
                return true;
            }
            return false;
        }

        private async Task CreateBirthdayTimer(UserProfile profile) {
            TryRemoveBirthdayTimer(profile);
            int nextYear = DateTime.Now.Year;
            DateTime relevantDay = new DateTime(nextYear, (int)profile.Borkday.Month, profile.Borkday.Day, 0, 0, 0);
            TimeSpan relevantOffset = GetRelevantTimeZone(profile, new DateTimeOffset(relevantDay)).TimeOffset;
            if (new DateTimeOffset(relevantDay, relevantOffset).CompareTo(DateTimeOffset.Now) <= 0) {
                nextYear++;
            }

            TimeSpan diff = new DateTimeOffset(new DateTime(nextYear, (int)profile.Borkday.Month, profile.Borkday.Day, 0, 0, 0), relevantOffset).Subtract(DateTimeOffset.Now);
            profile.BorkdayTimerToken = await CreateEventTimer(BorkdayCallback, new Dictionary<string, string>() { {"ID", profile.UserID.ToString()} }, (int)diff.TotalSeconds, Databases.EventTimers.TimerType.Expire, TimerService);
        }

        /// <summary>
        /// Adds the appropriate birthday role to a member for a day and resets the callback for the following year.
        /// </summary>
        /// <param name="args">A string-string dictionary containing a definition for a ulong-parsable "ID" UserID.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task BorkdayCallback(Dictionary<string, string> args) {

            ulong id = ulong.Parse(args["ID"]);
            IGuildUser user = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(id);
            if (user is null) return;

            UserProfile profile = ProfilesDB.GetOrCreateProfile(id);

            IRole role = Context.Guild.GetRole(
                user.GetPermissionLevel(DiscordSocketClient, BotConfiguration) >= PermissionLevel.Moderator ?
                    ModerationConfiguration.StaffBorkdayRoleID : ModerationConfiguration.BorkdayRoleID
            );

            await user.AddRoleAsync(role);

            // Notify friends with birthday notifications.

            await CreateEventTimer(
                RemoveBorkday,
                new Dictionary<string, string>() {
                    { "User", user.Id.ToString() },
                    { "Role", role.Id.ToString() }
                },
                ModerationConfiguration.SecondsOfBorkday,
                Databases.EventTimers.TimerType.Expire
            );

            await CreateBirthdayTimer(profile);

        }

        /// <summary>
        /// Removes a given role from a given user.
        /// </summary>
        /// <param name="args">A string-string dictionary containing a definition for a ulong-parsable "User" ID and a ulong-parsable "Role" ID.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task RemoveBorkday(Dictionary<string, string> args) {
            ulong userID = Convert.ToUInt64(args["User"]);
            ulong roleID = Convert.ToUInt64(args["Role"]);

            IGuild guild = DiscordSocketClient.GetGuild(BotConfiguration.GuildID);
            IGuildUser user = await guild?.GetUserAsync(userID);

            if (user is null) return;

            await user.RemoveRoleAsync(guild.GetRole(roleID));
        }
    }
}

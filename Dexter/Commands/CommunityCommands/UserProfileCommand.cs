using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.UserProfiles;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.Net;
using Humanizer;
using System.Globalization;
using System.Text;

namespace Dexter.Commands
{

    public partial class CommunityCommands
    {
        private const string AttributeNames = "Gender, Sexuality, Timezone, TimeZoneDST, DSTRules, Birthday, Birthyear, Nation, Languages and Miscellaneous.";

        /// <summary>
        /// Edits or queries data from a user's personal user profile.
        /// </summary>
        /// <param name="action">The action to take, either get or set.</param>
        /// <param name="parameters">In case of setting an attribute, the attribute to change followed by the value to set it to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("me")]
        [Alias("myprofile")]
        [Summary("View, edit, and configure your Social Dexter profile, everything from borkdays, timezones and custom user information.")]
        [ExtendedSummary("Usage: me ([action] [attribute] [value])\n" +
            "-  SET [attribute] [value]: Set one of your profile attributes to a given value.\n" +
            "-    Attributes are the following: " + AttributeNames + "\n" +
            "-    For Timezone attributes, the value should be a timezone abbreviation (use `~timezone search [abbr]` to check out similar abbreviations and meanings)\n" +
            "-    For TimeZone rules, follow the syntax `(from) [N]th (Weekday) (of) [Month] to [N]th (Weekday) (of) [Month]`\n" +
            "-  CONFIG [field] [value]: Configures your social profile preferences, such as privacy and friend requests.\n" +
            "-    To see all configuration fields, don't specify a field or value\n" +
            "-    To see values for a field, don't specify a value")]
        [BotChannel]

        public async Task MyProfileCommand(string action = "get", [Remainder] string parameters = "")
        {

            if (RestrictionsDB.IsUserRestricted(Context.User.Id, Databases.UserRestrictions.Restriction.Social))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User Restricted!")
                    .WithDescription("You don't have permissions to use the social module, if you think this is a mistake, please contact a moderator.")
                    .SendEmbed(Context.Channel);
                return;
            }

            UserProfile profile = ProfilesDB.GetOrCreateProfile(Context.User.Id);
            await ProfilesDB.Entry(profile).ReloadAsync();

            switch (action.ToLower())
            {
                case "get":
                case "profile":
                    await DisplayProfileInformation(profile);
                    return;
                case "set":
                case "edit":
                    int separatorIndex = parameters.IndexOf(' ');
                    if (separatorIndex < 0)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid Number of Parameters")
                            .WithDescription("You must provide at least an attribute name to change and a new value for it.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    string attribute = parameters[..separatorIndex];
                    string value = parameters[separatorIndex..].Trim();

                    if (value.Length > CommunityConfiguration.MaxProfileAttributeLength)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Excessively long value")
                            .WithDescription($"Keep your profile fields under a maximum length of {CommunityConfiguration.MaxProfileAttributeLength}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string feedback = "";
                    switch (attribute.ToLower())
                    {
                        case "timezone":
                        case "tz":
                            string[] segments;
                            char separator;
                            if (value.Contains('|'))
                                separator = '|';
                            else
                                separator = ' ';

                            segments = value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            TimeZoneData tz;
                            TimeZoneData tzdst;
                            DaylightShiftRules tzrules = null;

                            if (segments.Length == 1)
                            {
                                if (!TimeZoneData.TryParse(value, LanguageConfiguration, out tz))
                                {
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
                                    .WithDescription($"Your default and DST time zones have been set to {tz}.")
                                    .SendEmbed(Context.Channel);

                                await CreateBirthdayTimer(profile);
                                break;
                            }
                            else if (segments.Length >= 2)
                            {
                                bool tzParsed = TimeZoneData.TryParse(segments[0], LanguageConfiguration, out tz);
                                bool tzdstParsed = TimeZoneData.TryParse(segments[1], LanguageConfiguration, out tzdst);

                                if (!tzParsed || !tzdstParsed)
                                {
                                    await BuildEmbed(EmojiEnum.Annoyed)
                                        .WithTitle("Unable to find time zone!")
                                        .WithDescription($"I was unable to parse the following time zones:{(tzParsed ? "" : $" \"{segments[0]}\"")}{(tzdstParsed ? "" : $" \"{segments[1]}\"")}.\n" +
                                        $"Make sure you've capitalized it correctly. You can find similar time zone abbreviations by using `~timezone search [abbr]`.")
                                        .SendEmbed(Context.Channel);
                                    return;
                                }

                                bool rulesParsed = false;
                                string rulesError = "";
                                if (segments.Length >= 3)
                                {
                                    rulesParsed = DaylightShiftRules.TryParse(string.Join(separator, segments[2..]), LanguageConfiguration, out rulesError, out tzrules);
                                    if (rulesParsed)
                                    {
                                        profile.DstRules = tzrules;
                                    }
                                }

                                profile.TimeZone = segments[0];
                                profile.TimeZoneDST = segments[1];
                                await BuildEmbed(EmojiEnum.Love)
                                    .WithTitle("Successfully set time zones!")
                                    .WithDescription($"Your default and DST time zones have been set to **{tz}** and **{tzdst}** respectively." +
                                    (rulesParsed ? $"\nDST will be considered to be {tzrules}" : "No new DST rules were applied.") +
                                    (!rulesParsed && segments.Length >= 3 ? $"\nError while parsing DSTRules: {rulesError}" : ""))
                                    .SendEmbed(Context.Channel);
                                break;
                            }
                            else
                            {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Malformed Value")
                                    .WithDescription("Please type the value you wish to give to timezone in a valid format.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }
                        case "dst":
                        case "dsttz":
                        case "dsttimezone":
                        case "tzdst":
                        case "timezonedst":
                            if (value == "none")
                            {
                                profile.TimeZoneDST = "";
                                await GenericResetInfo("DST Time Zone");
                                return;
                            }

                            if (!TimeZoneData.TryParse(value, LanguageConfiguration, out TimeZoneData timeZoneDST))
                            {
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
                        case "timezonerules":
                        case "tzrules":
                            if (value == "none")
                            {
                                profile.DstRules = null;
                                await BuildEmbed(EmojiEnum.Sign)
                                    .WithTitle("Cleared data about DST application rules")
                                    .WithDescription($"All previous information about DST patterns has been removed. From now on, only your default time zone will be considered for any calculations.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            if (!DaylightShiftRules.TryParse(value, LanguageConfiguration, out feedback, out DaylightShiftRules rules))
                            {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to parse time zone rules")
                                    .WithDescription(feedback)
                                    .AddField("Format Info:", "Absolute: [N]th of [Month] to [N]th of [Month]. Relative: [N]th [Weekday] of [Month] to [N]th [Weekday] of [Month].")
                                    .AddField("Examples:", "24th of April to 8th of October; 1st Sunday of March to 2nd Sunday of September; last Sunday of Oct to first Monday of Feb.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            profile.DstRules = rules;
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Successfully set Local DST Rules")
                                .WithDescription(feedback)
                                .SendEmbed(Context.Channel);

                            await CreateBirthdayTimer(profile);
                            break;
                        case "borkday":
                        case "birthday":
                            if (value.ToLower() == "none")
                            {
                                profile.Borkday = default;
                                await TryRemoveBirthdayTimer(profile);
                                await BuildEmbed(EmojiEnum.Sign)
                                    .WithTitle("Removed local birthday records.")
                                    .WithDescription("Your birthday is no longer being tracked by the profile system.")
                                    .SendEmbed(Context.Channel);
                                break;
                            }

                            if (!DayInYear.TryParse(value, true, LanguageConfiguration, out DayInYear day, out feedback, out int year))
                            {
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
                            if (value.ToLower() == "none")
                            {
                                profile.BirthYear = default;
                                await BuildEmbed(EmojiEnum.Sign)
                                    .WithTitle("Removed local birth year records")
                                    .WithDescription("Your birth year is no longer being tracked by the profile system.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            if (!int.TryParse(value, out int result))
                            {
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
                            if (value == "none")
                            {
                                profile.Gender = "";
                                await GenericResetInfo("Gender");
                            }
                            else
                            {
                                profile.Gender = value;
                                await GenericSuccessInfo("Gender", value);
                            }
                            break;
                        case "sexuality":
                        case "orientation":
                            if (value == "none")
                            {
                                profile.Orientation = "";
                                await GenericResetInfo("Sexuality");
                            }
                            else
                            {
                                profile.Orientation = value;
                                await GenericSuccessInfo("Sexuality", value);
                            }
                            break;
                        case "sona":
                        case "fursona":
                        case "sonainfo":
                            if (value == "none")
                            {
                                profile.SonaInfo = "";
                                await GenericResetInfo("Sona Info");
                            }
                            else
                            {
                                profile.SonaInfo = value;
                                await GenericSuccessInfo("Sona Info", value);
                            }
                            break;
                        case "nation":
                        case "country":
                        case "nationality":
                            if (value == "none")
                            {
                                profile.Nationality = "";
                                await GenericResetInfo("Nationality");
                            }
                            else
                            {
                                profile.Nationality = value;
                                await GenericSuccessInfo("Nationality", value);
                            }
                            break;
                        case "languages":
                        case "language":
                            if (value == "none")
                            {
                                profile.Languages = "";
                                await GenericResetInfo("Languages");
                            }
                            else
                            {
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
                            if (value == "none")
                            {
                                profile.Info = "";
                                await GenericResetInfo("Info");
                            }
                            else
                            {
                                profile.Info = value;
                                await GenericSuccessInfo("Info", value);
                            }
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unknown Attribute")
                                .WithDescription($"Unknown attribute: \"{attribute}\". Currently valid attributes are: " + AttributeNames)
                                .SendEmbed(Context.Channel);
                            break;
                    }
                    return;
                case "settings":
                case "config":
                case "configuration":
                    if (string.IsNullOrEmpty(parameters))
                    {
                        if (profile.Settings is null)
                        {
                            profile.Settings = new();
                        }

                        await BuildEmbed(EmojiEnum.Sign)
                            .WithTitle("Profile Settings")
                            .WithDescription(
                            $"**Privacy** [*privacy*]: {profile.Settings.Privacy}\n" +
                            $"**Get Borkday Role on Borkday** [*borkdayrole*]: {(profile.Settings.GiveBorkdayRole ? "Yes" : "No")}\n" +
                            $"**Receive Friend Requests** [*friendrequests*]: {(profile.Settings.BlockRequests ? "Silent" : "Regular")}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    separatorIndex = parameters.IndexOf(' ');
                    if (separatorIndex < 0)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid Number of Parameters")
                            .WithDescription("You must provide at least an attribute name to change and a new value for it.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    attribute = parameters[..separatorIndex];
                    value = parameters[separatorIndex..].Trim();

                    if (value.Length > CommunityConfiguration.MaxProfileAttributeLength)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Excessively long value")
                            .WithDescription($"Keep your profile fields under a maximum length of {CommunityConfiguration.MaxProfileAttributeLength}")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    ProfilePreferences prefs = profile.Settings;

                    switch (attribute.ToLower())
                    {
                        case "privacy":
                        case "access":
                            switch (value.ToLower())
                            {
                                case "public":
                                case "everyone":
                                case "everybody":
                                    prefs.Privacy = ProfilePreferences.PrivacyMode.Public;
                                    await GenericSuccessInfo("Privacy", "Public; anyone will be able to view your profile.");
                                    break;
                                case "friends":
                                case "friends-only":
                                case "friendsonly":
                                    prefs.Privacy = ProfilePreferences.PrivacyMode.Friends;
                                    await GenericSuccessInfo("Privacy", "Friends-only; only your friends will be able to view your profile.");
                                    break;
                                case "private":
                                case "me":
                                    prefs.Privacy = ProfilePreferences.PrivacyMode.Private;
                                    await GenericSuccessInfo("Privacy", "Private; server members won't have access to your profile.");
                                    break;
                                default:
                                    await BuildEmbed(EmojiEnum.Sign)
                                        .WithTitle("Information about Privacy")
                                        .WithDescription("This setting dictates who can access your profile information by using the ~social command")
                                        .AddField("Possible Values",
                                        "Public: Anyone in the server can view your profile.\n" +
                                        "Friends: Only people you have friended with Dexter can view your profile.\n" +
                                        "Private: Your profile isn't accessible by members.")
                                        .SendEmbed(Context.Channel);
                                    return;
                            }
                            break;
                        case "borkdayrole":
                        case "birthdayrole":
                            switch (value.ToLower())
                            {
                                case "yes":
                                case "true":
                                    prefs.GiveBorkdayRole = true;
                                    await CreateBirthdayTimer(profile);
                                    await GenericSuccessInfo("Receive Borkday Role", "True");
                                    break;
                                case "no":
                                case "false":
                                    prefs.GiveBorkdayRole = false;
                                    await GenericSuccessInfo("Receive Borkday Role", "False");
                                    break;
                                default:
                                    await BuildEmbed(EmojiEnum.Sign)
                                        .WithTitle("Information about \"Receive Borkday Role\"")
                                        .WithDescription("This setting dictates whether you get the borkday role once your birthday is calculated to be in effect.")
                                        .AddField("Possible Values",
                                        "True: Activate the service.\n" +
                                        "False: Deactivate the service.")
                                        .SendEmbed(Context.Channel);
                                    return;
                            }
                            break;
                        case "friendrequests":
                        case "receivefriendrequests":
                            switch (value.ToLower())
                            {
                                case "yes":
                                case "true":
                                case "regular":
                                    prefs.BlockRequests = false;
                                    await GenericSuccessInfo("Receive Friend Requests", "True; you will receive Dexter DMs for active friend requests.");
                                    break;
                                case "no":
                                case "false":
                                case "silent":
                                    prefs.GiveBorkdayRole = true;
                                    await GenericSuccessInfo("Receive Friend Requests", "False; you will no longer receive DMs from Dexter when receiving friend requests.");
                                    break;
                                default:
                                    await BuildEmbed(EmojiEnum.Sign)
                                        .WithTitle("Information about \"Receive Friend Requests\"")
                                        .WithDescription("This setting dictates whether you get DMd by Dexter in sight of a new friend request.")
                                        .AddField("Possible Values",
                                        "True: Regular friend requests.\n" +
                                        "False: Silent friend requests.")
                                        .SendEmbed(Context.Channel);
                                    return;
                            }
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unknown Setting")
                                .WithDescription($"Unable to recognize \"{attribute} as a valid setting. Please use either the setting names displayed in `~me settings` between brackets.")
                                .SendEmbed(Context.Channel);
                            return;
                    }
                    profile.Settings = prefs;
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Invalid Action")
                        .WithDescription($"Unrecognized action: {action}, please use either \"get\", \"set\", or \"config\".")
                        .SendEmbed(Context.Channel);
                    return;
            }

            ProfilesDB.SaveChanges();
        }

        /// <summary>
        /// Display the social profile of a given user.
        /// </summary>
        /// <param name="user">The target user to query profile information for.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until it completes successfully.</returns>

        [Command("social")]
        [Alias("friend", "friends")]
        [Summary("Portal to the social link system; which mainly features the friends system.")]
        [ExtendedSummary("Usage: social (action) [user]\n" +
            "If you don't specify an action, you'll see the user's profile\n" +
            "-  ADD [user]: Sends or accepts a friend request.\n" +
            "-  REMOVE [user]: Removes a user as a friend or an outgoing friend request.\n" +
            "-  DECLINE [user]: Declines a friend request.\n" +
            "-  LIST: Shows a list of friends\n" +
            "-  REQUESTS: Shows a list of active friend requests\n" +
            "-  BLOCK [user]: Blocks a user, blocking friend requests from them\n" +
            "-  UNBLOCK [user]: Unblocks a user\n" +
            "-  BLOCKED: Shows a list of blocked users.")]
        [BotChannel]

        public async Task SocialQuery(IUser user)
        {

            if (RestrictionsDB.IsUserRestricted(user.Id, Databases.UserRestrictions.Restriction.Social))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User Restricted!")
                    .WithDescription("This user has had their access to the social system revoked, their profile isn't visible.")
                    .SendEmbed(Context.Channel);
                return;
            }

            UserProfile profile = ProfilesDB.GetOrCreateProfile(user.Id);

            bool allowed = Context.User.Id == user.Id || Context.User.GetPermissionLevel(DiscordShardedClient, BotConfiguration) >= PermissionLevel.Moderator;
            if (!allowed)
            {
                switch (profile.Settings.Privacy)
                {
                    case ProfilePreferences.PrivacyMode.Private:
                        allowed = false;
                        break;
                    case ProfilePreferences.PrivacyMode.Friends:
                        await ProfilesDB.Links.AsAsyncEnumerable().ForEachAsync(l =>
                        {
                            if (allowed) return;
                            if (l.LinkType != LinkType.Friend) return;
                            if (l.Sender == Context.User.Id && l.Sendee == user.Id) allowed = true;
                            else if (l.Sender == user.Id && l.Sendee == Context.User.Id) allowed = true;
                        });
                        break;
                    case ProfilePreferences.PrivacyMode.Public:
                        allowed = true;
                        break;
                }
            }

            if (!allowed)
            {
                await Context.Channel.SendMessageAsync("Unable to access profile! This user's profile is private.");
                return;
            }

            await DisplayProfileInformation(profile);
        }

        /// <summary>
        /// Alternative form of the Friend command that takes a user ID for DM compatibility
        /// </summary>
        /// <param name="action">The action to take on the user whose ID is <paramref name="userID"/></param>
        /// <param name="userID">The ID of the target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until it completes successfully.</returns>

        [Command("social")]
        [Alias("friend", "friends")]

        public async Task FriendCommand(string action, ulong userID)
        {
            IUser target = DiscordShardedClient.GetUser(userID);

            if (target is null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find user with ID {userID}.");
                return;
            }

            await FriendCommand(action, target);
        }

        /// <summary>
        /// Deals with all sorts of social linking, friending, blocking, etc.
        /// </summary>
        /// <param name="action">The action to take, add, remove, block, etc...</param>
        /// <param name="user">The user to affect by <paramref name="action"/>.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("social")]
        [Alias("friend", "friends")]

        public async Task FriendCommand(string action, IUser user = null)
        {
            if (user is not null && RestrictionsDB.IsUserRestricted(user.Id, Databases.UserRestrictions.Restriction.Social))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User Restricted!")
                    .WithDescription("This user has had their access to the social system revoked, their profile isn't accessible.")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (RestrictionsDB.IsUserRestricted(Context.User.Id, Databases.UserRestrictions.Restriction.Social))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User Restricted!")
                    .WithDescription("You don't have permissions to use the social module, if you think this is a mistake, please contact a moderator.")
                    .SendEmbed(Context.Channel);
                return;
            }

            UserProfile profile = null;

            if (user is not null)
            {
                profile = ProfilesDB.GetOrCreateProfile(user.Id);
            }

            if (user is not null && user.Id == Context.User.Id)
            {
                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle("Self-Centered much?")
                    .WithDescription("Look, I don't know what you're trying to do with yourself, but you should probably considering socializing with *other beings*.")
                    .SendEmbed(Context.Channel);
                return;
            }

            switch (action.ToLower())
            {
                case "list":
                    List<ulong> friends = await ProfilesDB.GetLinksAsync(Context.User.Id);
                    EmbedBuilder[] pages = BuildUserListEmbeds(friends, "Friends", "Here's a list of users you have friended using the Dexter social system:", FriendDisplay);

                    await PublishListResult(pages, "It seems you have no friends added yet! :c. You can add more using ~friend add [User]. But hey... I'm always here if you need a hug. <3");
                    return;
                case "requests":
                    List<ulong> friendRequests = await ProfilesDB.GetLinksAsync(Context.User.Id, false, LinkType.FriendRequest);
                    pages = BuildUserListEmbeds(friendRequests, "Friend Requests", "Here's a list of active friend requests linked with you!", FriendRequestDisplay);

                    await PublishListResult(pages, "Nothing to see here! You're up to date. ^w^");
                    return;
                case "add":
                case "accept":
                    if (user is null)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments")
                            .WithDescription("You must provide a user to add as a friend.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    UserLink req = ProfilesDB.GetLink(Context.User.Id, user.Id, true, true, LinkType.FriendRequest);
                    if (req is null)
                    {
                        req = ProfilesDB.GetLink(Context.User.Id, user.Id, true);

                        if (req is null)
                        {
                            req = ProfilesDB.GetOrCreateLink(Context.User.Id, user.Id, LinkType.FriendRequest);

                            if (profile.Settings.BlockRequests)
                            {
                                await BuildEmbed(EmojiEnum.Unknown)
                                    .WithTitle("User has silent friend requests!")
                                    .WithDescription($"User {user.Mention} now has a pending friend request from you, but they weren't notified of it. They can check their active friend requests with `{BotConfiguration.Prefix}friend requests`.\n" +
                                    $"To accept your request, they must use the `{BotConfiguration.Prefix}friend add {Context.User.Id}` command.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }

                            try
                            {
                                await BuildEmbed(EmojiEnum.Love)
                                    .WithTitle("Incoming Friend Request!")
                                    .WithThumbnailUrl(Context.User.GetTrueAvatarUrl())
                                    .WithAuthor(Context.User)
                                    .WithDescription($"You've received a friend request from {Context.User.Mention}!\n" +
                                    $"To accept or decline this request, type `~friend <accept|decline> {Context.User.Id}`.")
                                    .SendEmbed(await user.CreateDMChannelAsync());

                                await BuildEmbed(EmojiEnum.Love)
                                    .WithTitle("Friend Request Sent!")
                                    .WithDescription($"{user.Mention} has been notified of your request. You can check the list of friends you have by typing `~friend list`.")
                                    .SendEmbed(Context.Channel);
                            }
                            catch (HttpException)
                            {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Unable to Send Message!")
                                    .WithDescription($"I wasn't able to contact {user.Mention}, perhaps you could contact them yourself? They can accept this request by using the command `~friend add {Context.User.Id}`.")
                                    .SendEmbed(Context.Channel);
                            }
                            return;
                        }

                        if (req.LinkType == LinkType.Friend)
                        {
                            await BuildEmbed(EmojiEnum.Wut)
                                .WithTitle("You're already friends with this user!")
                                .WithDescription($"You and {user.Mention} are already friends!")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                        else if (req.LinkType == LinkType.Blocked)
                        {
                            if (req.Sender == Context.User.Id && req.Settings.BlockMode.HasFlag(Databases.UserProfiles.Direction.Sender)
                                || req.Sendee == Context.User.Id && req.Settings.BlockMode.HasFlag(Databases.UserProfiles.Direction.Sendee))
                            {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("This user has blocked you.")
                                    .WithDescription("You can't send notifications to a user that has blocked you.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }
                            else
                            {
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("You have blocked this user!")
                                    .WithDescription($"You can't send notifications to a user you have blocked; to unblock them, use `~social unblock {user.Id}`.")
                                    .SendEmbed(Context.Channel);
                                return;
                            }
                        }
                    }

                    if (req.Sender == Context.User.Id)
                    {
                        await BuildEmbed(EmojiEnum.Wut)
                            .WithTitle("Active Friend Request Present")
                            .WithDescription($"You've already sent {user.Mention} a friend request. They can accept it by using the `~friend add {Context.User.Id}` command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (req.Sender != Context.User.Id && req.LinkType == LinkType.FriendRequest)
                    {
                        req.LinkType = LinkType.Friend;
                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Accepted Friend Request!")
                            .WithDescription($"You are now friends with {user.Mention}! ^w^")
                            .SendEmbed(Context.Channel);
                        break;
                    }
                    break;
                case "remove":
                case "unfriend":
                    if (user is null)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments")
                            .WithDescription("You must provide a user to remove your friendship status with.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (!ProfilesDB.TryRemove(Context.User.Id, user.Id))
                    {
                        if (!ProfilesDB.TryRemove(Context.User.Id, user.Id, false, LinkType.FriendRequest))
                        {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Non-existent Link")
                                .WithDescription("You and the specified user are not friends in the Dexter system.")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                        else
                        {
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Removed Friend Request!")
                                .WithDescription($"Your friend request to {user.Mention} has been cancelled.")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                    }

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Removed Friend")
                        .WithDescription($"Your friend status with user {user.Mention} has been revoked.")
                        .SendEmbed(Context.Channel);
                    return;
                case "decline":
                    if (user is null)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments")
                            .WithDescription("You must provide a user to remove your friendship status with.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (!ProfilesDB.TryRemove(user.Id, Context.User.Id, false, LinkType.FriendRequest))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Non-existent Request")
                            .WithDescription("This user hasn't sent you a friend request. :(")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    else
                    {
                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Removed Friend Request!")
                            .WithDescription($"You have declined the friend request from {user.Mention}.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                case "block":
                    if (user is null)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments")
                            .WithDescription("You must provide a user to block.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    UserLink toBlock = ProfilesDB.GetOrCreateLink(Context.User.Id, user.Id, LinkType.Blocked);

                    if (toBlock.LinkType == LinkType.Friend)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("User is friended!")
                            .WithDescription($"You can't block friends. If you wish to block this user, unfriend them first: `~friend remove {user.Id}`.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    toBlock.LinkType = LinkType.Blocked;

                    if (toBlock.IsUserBlocked(user.Id))
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("User already blocked")
                            .WithDescription($"You've already blocked user {user.Mention}.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    else
                    {
                        toBlock.BlockUser(user.Id);

                        await BuildEmbed(EmojiEnum.Sign)
                            .WithTitle("User Successfully Blocked")
                            .WithDescription($"You've blocked {user.Mention}, you won't receive any Dexter notifications from them.")
                            .SendEmbed(Context.Channel);
                        break;
                    }
                case "unblock":
                    if (user is null)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments")
                            .WithDescription("You must provide a user to unblock.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    UserLink link = ProfilesDB.GetLink(Context.User.Id, user.Id, true, true, LinkType.Blocked);

                    if (link is null)
                    {
                        await BuildEmbed(EmojiEnum.Wut)
                            .WithTitle("No block link found.")
                            .WithDescription("You can't unblock a user that you haven't blocked.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    bool blockedSendee = link.Sender == Context.User.Id && link.Settings.BlockMode.HasFlag(Databases.UserProfiles.Direction.Sendee);
                    bool blockedSender = link.Sendee == Context.User.Id && link.Settings.BlockMode.HasFlag(Databases.UserProfiles.Direction.Sender);
                    if (blockedSendee || blockedSender)
                    {
                        if (blockedSendee) link.Settings.BlockMode &= ~Databases.UserProfiles.Direction.Sendee;
                        if (blockedSender) link.Settings.BlockMode &= ~Databases.UserProfiles.Direction.Sender;
                        bool isUserBlocked = link.Settings.BlockMode != Databases.UserProfiles.Direction.None;

                        await BuildEmbed(isUserBlocked ? EmojiEnum.Sign : EmojiEnum.Love)
                            .WithTitle("User Unblocked")
                            .WithDescription($"You've unblocked user {user.Mention}!{(isUserBlocked ? " However, they still have you blocked" : "")}.")
                            .SendEmbed(Context.Channel);
                        break;
                    }
                    else
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Incongruent Block Authority")
                            .WithDescription($"{user.Mention} has blocked you, but you haven't blocked them. You can't remove a block imposed by a different user.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                case "blocked":
                    List<ulong> blockedUsers = await ProfilesDB.GetLinksAsync(Context.User.Id, false, LinkType.Blocked);
                    pages = BuildUserListEmbeds(blockedUsers, "Blocked Users", "Here's a list of users you have blocked or that have blocked you:", BlockedDisplay);

                    await PublishListResult(pages, "Nobody! Quite impressive.");
                    return;
                default:

                    return;
            }
            await ProfilesDB.SaveChangesAsync();
        }

        /// <summary>
        /// Configures a specific link between two users.
        /// </summary>
        /// <param name="user">The target user of the link to modify.</param>
        /// <param name="attribute">The attribute in the configuration to modify.</param>
        /// <param name="value">The value to give to the attribute.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("configlink")]
        [Alias("linkconfig")]
        [Summary("Configures the link-specific settings of a command.")]
        [ExtendedSummary("Usage: configlink [user] (attribute) (value)\n" +
            "Leave attribute and value empty to see your current settings for a social link.\n" +
            "In this information report, you can also see the possible configuration attributes.\n" +
            "Leave the value empty to see the possible values for an attribute.")]
        [BotChannel]

        public async Task ConfigLinkCommand(IUser user, string attribute = "", [Remainder] string value = "")
        {

            if (RestrictionsDB.IsUserRestricted(Context.User.Id, Databases.UserRestrictions.Restriction.Social))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User Restricted!")
                    .WithDescription("You don't have permissions to use the social module, if you think this is a mistake, please contact a moderator.")
                    .SendEmbed(Context.Channel);
                return;
            }

            UserLink link = ProfilesDB.GetLink(Context.User.Id, user.Id, true, true);

            if (link is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User Link not found")
                    .WithDescription($"You aren't friended with {user.Mention}, you can send them a friend request with `~friend add {user.Id}` if you haven't.")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (string.IsNullOrEmpty(attribute))
            {
                await BuildEmbed(EmojiEnum.Unknown)
                    .WithThumbnailUrl(user.GetTrueAvatarUrl())
                    .WithTitle($"Friend Settings")
                    .WithDescription($"Link settings with user {user.Mention}:\n" +
                    $"Get Borkday Notification [*borkdaynotify*]: {(link.IsUserBorkdayNotified(Context.User.Id) ? "True" : "False")}.\n")
                    .SendEmbed(Context.Channel);
                return;
            }

            LinkPreferences prefs = link.Settings;

            switch (attribute.ToLower())
            {
                case "borkdaynotify":
                case "birthdaynotify":
                    switch (value.ToLower())
                    {
                        case "true":
                        case "yes":
                            prefs.SetBorkdayMode(link, Context.User.Id, true);
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Set value for \"Borkday Notify\"")
                                .WithDescription("You will now receive birthday notifications from this user if they have their settings configured.")
                                .SendEmbed(Context.Channel);
                            break;
                        case "false":
                        case "no":
                            prefs.SetBorkdayMode(link, Context.User.Id, false);
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Set value for \"Borkday Notify\"")
                                .WithDescription("You will no longer receive birthday notifications from this user.")
                                .SendEmbed(Context.Channel);
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Sign)
                                .WithTitle("Information about Borkday Notify")
                                .WithDescription("This setting controls whether you get birthday notifications for your friend's birthday.")
                                .AddField("Possible Values",
                                "True: Receive borkday notifications.\n" +
                                "False: Don't receive borkday notifications.")
                                .SendEmbed(Context.Channel);
                            return;
                    }
                    break;
                default:

                    return;
            }
            link.Settings = prefs;
            await ProfilesDB.SaveChangesAsync();
        }

        /// <summary>
        /// Configures a specific link between two users.
        /// </summary>
        /// <param name="userID">The ID of the target user of the link to modify.</param>
        /// <param name="attribute">The attribute in the configuration to modify.</param>
        /// <param name="value">The value to give to the attribute.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("configlink")]
        [BotChannel]

        public async Task ConfigLinkCommand(ulong userID, string attribute = "", [Remainder] string value = "")
        {
            IUser target = DiscordShardedClient.GetUser(userID);

            if (target is null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find user with ID {userID}.");
                return;
            }

            await ConfigLinkCommand(target, attribute, value);
        }

        private async Task PublishListResult(EmbedBuilder[] pages, string emptySetRemark)
        {
            if (pages.Length == 0)
            {
                await Context.Channel.SendMessageAsync(emptySetRemark);
            }
            else if (pages.Length == 1)
            {
                await pages.First().SendEmbed(Context.Channel);
            }
            else
            {
                await CreateReactionMenu(pages, Context.Channel);
            }
            return;
        }

        private async Task DisplayProfileInformation(UserProfile profile)
        {
            IUser user = DiscordShardedClient.GetUser(profile.UserID);

            if (user is null) { await Context.Channel.SendMessageAsync("Unable to find user information!"); }

            bool TZSuccess = TimeZoneData.TryParse(profile.TimeZone, LanguageConfiguration, out TimeZoneData TZ);
            bool TZDSTSuccess = TimeZoneData.TryParse(profile.TimeZoneDST, LanguageConfiguration, out TimeZoneData TZDST);

            await BuildEmbed(EmojiEnum.Unknown)
                .WithThumbnailUrl(user.GetTrueAvatarUrl())
                .WithTitle($"{((user as IGuildUser)?.Nickname ?? user.Username).Possessive()} Profile")
                .WithDescription($"Custom user profile for user: {user.GetUserInformation()}")
                .AddField(!string.IsNullOrEmpty(profile.Gender), "Gender", profile.Gender, true)
                .AddField(!string.IsNullOrEmpty(profile.Orientation), "Sexuality", profile.Orientation, true)
                .AddField(profile.Borkday != default, "Borkday", $"{profile.Borkday}" + (profile.BirthYear > 0 ? $", {profile.BirthYear} (**Age**: {GetAge(profile, out _)})" : ""))
                .AddField(TZSuccess, "Time Zone", TZ.ToString() + ((profile.TimeZone != profile.TimeZoneDST && TZDSTSuccess) ? $" ({TZDST} during Daylight Savings. [{profile.DstRules?.ToString() ?? "Never"}])" : ""))
                .AddField(!string.IsNullOrEmpty(profile.Nationality), "Nationality", profile.Nationality, true)
                .AddField(!string.IsNullOrEmpty(profile.Languages), "Languages", profile.Languages, true)
                .AddField(!string.IsNullOrEmpty(profile.SonaInfo), "Sona", profile.SonaInfo)
                .AddField(!string.IsNullOrEmpty(profile.Info), "Info", profile.Info)
                .SendEmbed(Context.Channel);
        }

        private async Task GenericSuccessInfo(string attribute, object value)
        {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Successfully changed value of \"{attribute}\"")
                .WithDescription($"New value set to: {value}")
                .SendEmbed(Context.Channel);
        }

        private async Task GenericResetInfo(string attribute)
        {
            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle($"Cleared local data about \"{attribute}\".")
                .WithDescription("The data you've previously introduced for this field has been removed and is no longer being tracked.")
                .SendEmbed(Context.Channel);
        }

        private string GetAge(UserProfile profile, out TimeSpan age)
        {
            age = default;

            if (profile.BirthYear is null || profile.BirthYear <= 0)
            {
                return "Insufficient information; Missing birth year.";
            }
            else
            {
                if (profile.Borkday != default)
                {
                    age = DateTimeOffset.Now.Subtract(GetBirthDay(profile));
                    return $"{age.Humanize(3, maxUnit: Humanizer.Localisation.TimeUnit.Year, minUnit: Humanizer.Localisation.TimeUnit.Day)}";
                }
                int yrs = DateTime.Now.Year - (int)profile.BirthYear;
                age = TimeSpan.FromDays(yrs * 365.24);
                return $"Approximately {yrs} years.";
            }
        }

        private DateTimeOffset GetNow(UserProfile profile)
        {
            return DateTimeOffset.Now.ToOffset(profile.GetRelevantTimeZone(LanguageConfiguration).TimeOffset);
        }

        private DateTimeOffset GetBirthDay(UserProfile profile)
        {
            int day = profile.Borkday?.Day ?? 0;
            LanguageHelper.Month month = profile.Borkday?.Month ?? LanguageHelper.Month.January;
            DateTime birthday;

            if (day == 29 && month == LanguageHelper.Month.February
                && !CultureInfo.InvariantCulture.Calendar.IsLeapYear(profile.BirthYear ?? DateTime.Now.Year))
            {
                day = 1;
                month = LanguageHelper.Month.March;
            }
            birthday = new DateTime(profile.BirthYear ?? DateTime.Now.Year, (int)month, day, 0, 0, 0);

            return new DateTimeOffset(birthday, profile?.GetRelevantTimeZone(LanguageConfiguration).TimeOffset ?? default);
        }

        private async Task<bool> TryRemoveBirthdayTimer(UserProfile profile)
        {
            if (!string.IsNullOrEmpty(profile.BorkdayTimerToken) && TimerService.TimerExists(profile.BorkdayTimerToken))
            {
                await TimerService.RemoveTimer(profile.BorkdayTimerToken);
                return true;
            }
            return false;
        }

        private async Task CreateBirthdayTimer(UserProfile profile)
        {
            if (profile?.Borkday is null)
            {
                return;
            }

            await TryRemoveBirthdayTimer(profile);
            TimeSpan diff = TimeSpan.Zero;

            int nextYear = DateTime.Now.Year - 1;
            int monthNow = DateTime.Now.Month;
            int dayNow = DateTime.Now.Day;
            int monthBD = (int)profile.Borkday.Month;
            int dayBD = profile.Borkday.Day;

            while (diff <= TimeSpan.Zero)
            {
                nextYear++;
                if (monthBD == (int)LanguageHelper.Month.February && dayBD == 29
                    && !CultureInfo.InvariantCulture.Calendar.IsLeapYear(nextYear))
                {
                    monthBD++;
                    dayBD = 1;
                }
                DateTime relevantDay = new(nextYear, monthBD, dayBD, 0, 0, 0);
                TimeSpan relevantOffset = profile.GetRelevantTimeZone(new DateTimeOffset(relevantDay), LanguageConfiguration).TimeOffset;

                diff = new DateTimeOffset(relevantDay, relevantOffset).Subtract(DateTimeOffset.Now);
            }

            profile.BorkdayTimerToken = await CreateEventTimer(BorkdayCallback, new Dictionary<string, string>() { { "ID", profile.UserID.ToString() } }, (int)diff.TotalSeconds, Databases.EventTimers.TimerType.Expire, TimerService);
        }

        /// <summary>
        /// Adds the appropriate birthday role to a member for a day and resets the callback for the following year.
        /// </summary>
        /// <param name="args">A string-string dictionary containing a definition for a ulong-parsable "ID" UserID.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task BorkdayCallback(Dictionary<string, string> args)
        {

            ulong id = ulong.Parse(args["ID"]);

            if (RestrictionsDB.IsUserRestricted(id, Databases.UserRestrictions.Restriction.Social))
            {
                return;
            }

            IGuildUser user = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(id);
            if (user is null) return;

            UserProfile profile = ProfilesDB.GetOrCreateProfile(id);

            if (profile.Settings?.GiveBorkdayRole ?? false)
            {
                IRole role = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetRole(
                    user.GetPermissionLevel(DiscordShardedClient, BotConfiguration) >= PermissionLevel.Moderator ?
                        ModerationConfiguration.StaffBorkdayRoleID : ModerationConfiguration.BorkdayRoleID
                );

                await user.AddRoleAsync(role);

                await CreateEventTimer(
                    RemoveBorkday,
                    new Dictionary<string, string>() {
                        { "User", user.Id.ToString() },
                        { "Role", role.Id.ToString() }
                    },
                    ModerationConfiguration.SecondsOfBorkday,
                    Databases.EventTimers.TimerType.Expire
                );

                try
                {
                    await BuildEmbed(EmojiEnum.Unknown)
                        .WithColor(Color.Gold)
                        .WithThumbnailUrl(user.GetTrueAvatarUrl())
                        .WithTitle("🎂 Happy Borkday 🎂")
                        .WithDescription("Happy borkday from the USF Team! We hope your day goes wonderfully!\n" +
                        "If this wasn't sent at midnight, make sure you set your timezones up correctly on your profile :3")
                        .SendEmbed(await user.CreateDMChannelAsync());
                }
                catch { }
            }

            List<ulong> notify = await ProfilesDB.BorkdayNotifs(user.Id);
            foreach (ulong uid in notify)
            {
                try
                {
                    IUser friend = DiscordShardedClient.GetUser(uid);
                    await BuildEmbed(EmojiEnum.Unknown)
                        .WithColor(Color.Gold)
                        .WithThumbnailUrl(user.GetTrueAvatarUrl())
                        .WithTitle("🎂 Borkday Time 🎂")
                        .WithDescription($"Hey there! It's {user.Mention}'{(user.Username.EndsWith('s') ? "" : "s")} birthday! Thought you'd like to know and celebrate! :3")
                        .SendEmbed(await friend.CreateDMChannelAsync());
                }
                catch { }
            }

            await CreateBirthdayTimer(profile);

        }

        /// <summary>
        /// Removes a given role from a given user.
        /// </summary>
        /// <param name="args">A string-string dictionary containing a definition for a ulong-parsable "User" ID and a ulong-parsable "Role" ID.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task RemoveBorkday(Dictionary<string, string> args)
        {
            ulong userID = Convert.ToUInt64(args["User"]);
            ulong roleID = Convert.ToUInt64(args["Role"]);

            IGuild guild = DiscordShardedClient.GetGuild(BotConfiguration.GuildID);
            IGuildUser user = await guild?.GetUserAsync(userID);

            if (user is null) return;

            await user.RemoveRoleAsync(guild.GetRole(roleID));
        }

        /// <summary>
        /// Creates an collection of <see cref="EmbedBuilder"/>s that list a set of <paramref name="users"/> with a specific display per-user.
        /// </summary>
        /// <param name="users">The set of users to display.</param>
        /// <param name="title">The title of the list menu.</param>
        /// <param name="description">The description before the list of users is displayed.</param>
        /// <param name="displayPattern">A method detailing the way to display each user as a string.</param>
        /// <returns>An array of <see cref="EmbedBuilder"/>s, detailing the given list of <paramref name="users"/>.</returns>

        public EmbedBuilder[] BuildUserListEmbeds(IEnumerable<ulong> users, string title, string description, Func<IUser, string> displayPattern)
        {
            List<EmbedBuilder> embeds = new();
            int inPage = CommunityConfiguration.MaxUsersPerEmbed;
            List<StringBuilder> userlists = new();

            foreach (ulong userID in users)
            {
                IUser user = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(userID);

                if (user is null)
                {
                    user = DiscordShardedClient.GetUser(userID);
                    if (user is null) continue;
                }

                if (inPage >= CommunityConfiguration.MaxUsersPerEmbed)
                {
                    userlists.Add(new());
                    inPage = 0;
                }

                userlists.Last().Append($"{displayPattern(user)}\n");
                inPage++;
            }

            foreach (StringBuilder sb in userlists)
            {
                embeds.Add(
                    BuildEmbed(EmojiEnum.Unknown)
                    .WithTitle(title)
                    .WithDescription($"{description}\n {sb}")
                );
            }

            return embeds.ToArray();
        }

        private static string DefaultDisplay(IUser user)
        {
            return $"👥{user.Mention}";
        }

        private string BlockedDisplay(IUser user)
        {
            UserLink link = ProfilesDB.GetOrCreateLink(Context.User.Id, user.Id, LinkType.Invalid);
            bool receiving = link.IsUserBlocked(Context.User.Id);
            bool blocking = link.IsUserBlocked(user.Id);
            return $"🚫{user.Mention} ({(receiving ? "⬅️" : "")}{(blocking ? "➡️" : "")})";
        }

        private string FriendDisplay(IUser user)
        {
            UserLink link = ProfilesDB.GetOrCreateLink(Context.User.Id, user.Id, LinkType.Invalid);
            bool borkdayNotifs = link.IsUserBorkdayNotified(Context.User.Id);
            return $"👥{user.Mention}{(borkdayNotifs ? " (🎂)" : "")}";
        }

        private string FriendRequestDisplay(IUser user)
        {
            UserLink link = ProfilesDB.GetOrCreateLink(Context.User.Id, user.Id, LinkType.Invalid);
            bool sent = link.Sender == Context.User.Id;
            return $"{(sent ? "➡️" : "⬅️")}{user.Mention} {(sent ? "(Outgoing)" : $"(Incoming: `~friend <add|decline> {user.Id}`)")}";
        }
    }
}

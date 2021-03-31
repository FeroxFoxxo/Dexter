using Dexter.Attributes.Methods;
using Dexter.Databases.CommunityEvents;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class CommunityCommands {

        const string TimeEventSeparator = ";";

        /// <summary>
        /// Manages the event suggestion system for user-hosted events.
        /// </summary>
        /// <param name="Action">An action to execute on <paramref name="Params"/>, "Add", "Remove", "Edit", or "Get".</param>
        /// <param name="Params">The remainder of the command to modify or target events in the database.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("event", RunMode = RunMode.Async)]
        [Summary("Create and manage user-hosted events!\n" +
            ">`ADD [TIME] " + TimeEventSeparator + " [EVENT]` - create a new event!\n" +
            ">`REMOVE [EVENT ID]` - remove an event (proposer or admin only)\n" +
            ">`EDIT [EVENT ID] [NEW EVENT]` - edit an event's description (proposer or admin only)\n" +
            ">`[APPROVE/DECLINE] [EVENT ID] (REASON)` - approve or decline an event and optionally give a reason (staff only)\n" +
            ">`GET [PARAMTYPE] [PARAM]` - shows description, proposer, and time remaining until an event is released.")]
        [ExtendedSummary("Create and manage user-hosted events!\n" +
            ">`ADD [TIME] " + TimeEventSeparator + " [EVENT]` - create a new event!\n" +
            ">`REMOVE [EVENT ID]` - remove an event (proposer or admin only)\n" +
            ">`EDIT [EVENT ID] [NEW EVENT]` - edit an event's description (proposer or admin only)\n" +
            ">`[APPROVE/DECLINE] [EVENT ID] (REASON)` - approve or decline an event and optionally give a reason (staff only)\n" +
            ">`GET [PARAMTYPE] [PARAM]` - shows description, proposer, and time remaining until an event is released.\n" +
            "-->`GET ID [TOPIC ID]` - gets a topic by ID\n" +
            "-->`GET USER [USER]` - gets a set of topics by proposer\n" +
            "-->`GET <DESC or DESCRIPTION> [DESCRIPTION]` - gets a topic by its text output\n" +
            "Notes: \n" +
            "\tTime must be given as follows: `(date) [time] [time zone]` where date = `mm/dd(/year)` or `month dd(, year)`; time = `hh:mm(:ss)`; and time zone is an expression such as \"EDT\" or an offset to UTC (e.g. -4:00 or +1:00), elements in parentheses are optional, if am/pm isn't provided, 24h is used.")]
        [Alias("communityevent", "events")]
        [BotChannel]

        public async Task EventCommand(string Action, [Remainder] string Params) {

            CommunityEvent Event;
            string EventParam;

            IGuildUser User = Context.User as IGuildUser;
            if(Context.Channel is IDMChannel) {
                User = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(Context.User.Id);

                if(User == null) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Couldn't find you in the server!")
                        .WithDescription($"Are you sure you are in USF? If this continues to fail, consider running this command in <#{BotConfiguration.BotChannels[0]}> instead.")
                        .SendEmbed(Context.Channel);
                    return;
                }
            }

            if (!Enum.TryParse(Action.ToLower().Pascalize(), out Enums.ActionType ActionType)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Action Parse Error!")
                    .WithDescription($"Action \"{Action}\" not found! Please use `ADD`, `GET`, `REMOVE`, or `EDIT`")
                    .SendEmbed(Context.Channel);
                return;
            }

            switch (ActionType) {
                case Enums.ActionType.Add:
                    if (RestrictionsDB.IsUserRestricted(Context.User, Databases.UserRestrictions.Restriction.Events)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You aren't permitted to create events!")
                            .WithDescription("You have been blacklisted from using this service. If you think this is a mistake, feel free to personally contact an administrator.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string ReleaseArg = Params.Split(TimeEventSeparator)[0];
                    if(!LanguageHelper.TryParseTime(ReleaseArg.Trim(), CultureInfo.CurrentCulture, LanguageConfiguration, out DateTimeOffset ReleaseTime, out _)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to parse time!")
                            .WithDescription($"I was unable to parse the time: `{ReleaseArg}`\n Make sure it follows the correct format! For more info, check out `{BotConfiguration.Prefix}checktime [Your Date]`")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if(ReleaseArg.Length + 1 >= Params.Length) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to parse event!")
                            .WithDescription($"I found event parameters: `{Params}`. \nYou must separate time and description with a semicolon ({TimeEventSeparator})!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string Description = Params[(ReleaseArg.Length + TimeEventSeparator.Length)..].Trim();

                    await AddEvent(EventType.UserHosted, User, ReleaseTime, Description);
                    break;
                case Enums.ActionType.Remove: 
                    Event = await ValidateCommunityEventByID(Params);
                    if (Event == null) return;

                    if (!(User.Id == Event.ProposerID || User.GetPermissionLevel(DiscordSocketClient, BotConfiguration) == PermissionLevel.Administrator)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Missing permissions!")
                            .WithDescription("Only administrators or the proposer of the event can remove the event.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    await RemoveEvent(Event.ID);
                    break;
                case Enums.ActionType.Edit:
                    if (RestrictionsDB.IsUserRestricted(Context.User, Databases.UserRestrictions.Restriction.Events)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You aren't permitted to modify events!")
                            .WithDescription("You have been blacklisted from using this service. If you think this is a mistake, feel free to personally contact an administrator.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    EventParam = Params.Split(" ")[0];
                    Event = await ValidateCommunityEventByID(EventParam);
                    if (Event == null) return;

                    if (!(User.Id == Event.ProposerID || User.GetPermissionLevel(DiscordSocketClient, BotConfiguration) == PermissionLevel.Administrator)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Missing permissions!")
                            .WithDescription("Only administrators or the proposer of the event can edit the event.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    await EditEvent(Event.ID, Params[EventParam.Length..].Trim());
                    break;
                case Enums.ActionType.Approve:
                case Enums.ActionType.Decline:
                    EventParam = Params.Split(" ")[0];
                    Event = await ValidateCommunityEventByID(EventParam);
                    if (Event == null) return;

                    if (User.GetPermissionLevel(DiscordSocketClient, BotConfiguration) < PermissionLevel.Moderator) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Oop! Don't go there~")
                            .WithDescription("Only staff can modify the status of an event suggestion!")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string Reason = Params[EventParam.Length..].Trim();

                    await ResolveEventProposal(Event.ID, Reason, ActionType);
                    break;
                case Enums.ActionType.Get:
                    string SearchParam = Params.Split(" ")[0].ToUpper();
                    string SearchString = Params[SearchParam.Length..].Trim();
                    List<CommunityEvent> Events = new List<CommunityEvent>();
                    switch (SearchParam) {
                        case "ID":
                            Events.Add(await ValidateCommunityEventByID(SearchString));
                            if (Events[0] == null) return;                            
                            break;
                        case "USER":
                            IUser TargetUser;
                            TargetUser = Context.Message.MentionedUsers.FirstOrDefault();
                            if(TargetUser == null && ulong.TryParse(SearchString, out ulong TargetID)) {
                                TargetUser = DiscordSocketClient.GetUser(TargetID);
                            }

                            if (TargetUser != null) Events.AddRange(GetEvents(TargetUser));
                            break;
                        case "DESC":
                        case "DESCRIPTION":
                            Events.Add(await ValidateCommunityEventByDescription(SearchString));
                            if (Events[0] == null) return;
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Parameter Parse Error!")
                                .WithDescription($"Search Parameter \"{SearchParam}\" is invalid! Use `ID`, `USER`, or `DESCRIPTION`.")
                                .SendEmbed(Context.Channel);
                            break;
                    }

                    if (Events.Count == 0) {
                        await BuildEmbed(EmojiEnum.Wut)
                            .WithTitle("No Events Found!")
                            .WithDescription("No events are compatible with the filters you set for the search.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    else if (Events.Count == 1) {
                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle("1 Event Found!")
                            .WithDescription($"**{(Events[0].EventType == EventType.Official ? "Official" : "Community")} Event #{Events[0].ID}:** \n{Events[0].Description}")
                            .AddField("Author:", DiscordSocketClient.GetUser(Events[0].ProposerID).GetUserInformation())
                            .AddField("Time Proposed:", DateTimeOffset.FromUnixTimeSeconds(Events[0].DateTimeProposed).Humanize(), true)
                            .AddField("Status:", Events[0].Status.ToString(), true)
                            .AddField("Release Time:", DateTimeOffset.FromUnixTimeSeconds(Events[0].DateTimeRelease).Humanize(), true)
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    else {
                        await CreateReactionMenu(GenerateUserEventsMenu(Events.ToArray()), Context.Channel);
                        return;
                    }
            }
        }

        /// <summary>
        /// Creates an official server event given a stringified date and description.
        /// </summary>
        /// <param name="Args">A string containing a parsable DateTimeOffset and a Description separated by TimeEventSeparator.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("officialevent", RunMode = RunMode.Async)]
        [Summary("Creates an official server event!\n" +
            "Syntax: `officialevent [TIME] " + TimeEventSeparator + " [DESCRIPTION]`")]
        [ExtendedSummary("Creates an official server event! \n" + 
            "Syntax: `officialevent [TIME] " + TimeEventSeparator + " [DESCRIPTION]` \n" +
            "Notes: Time must be given as follows: `(dd/mm/yyyy) hh:mm(:ss) (<am/pm>) <+/->tz`, elements in parentheses are optional, if am/pm isn't provided, 24h is used. tz is the time zone (e.g. -4:00 for EDT or +1:00 for CET)")]
        [Alias("serverevent")]
        [RequireModerator]

        public async Task OfficialEventCommand([Remainder] string Args) {
            string ReleaseArg = Args.Split(TimeEventSeparator)[0];
            DateTimeOffset ReleaseTime = DateTimeOffset.Parse(ReleaseArg.Trim());
            string Description = Args[(ReleaseArg.Length + TimeEventSeparator.Length)..].Trim();

            await AddEvent(EventType.Official, Context.User as IGuildUser, ReleaseTime, Description);
        }

        private async Task<CommunityEvent> ValidateCommunityEventByID(string ArgID) {
            if (!int.TryParse(ArgID, out int EventID)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("ID Parse Error!")
                    .WithDescription($"ID \"{ArgID}\" is not a valid number!")
                    .SendEmbed(Context.Channel);
                return null;
            }
            CommunityEvent Event = GetEvent(EventID);
            if (Event == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Event ID!")
                    .WithDescription($"ID \"{EventID}\" has no event associated with it.!")
                    .SendEmbed(Context.Channel);
                return null;
            }

            return Event;
        }

        private async Task<CommunityEvent> ValidateCommunityEventByDescription(string Description) {
            CommunityEvent Event = GetEvent(Description);
            if(Event == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Event Description!")
                    .WithDescription($"No events exist with the following description: `{Description}`")
                    .SendEmbed(Context.Channel);
                return null;
            }

            return Event;
        }
    }
}

using Dexter.Databases.CommunityEvents;
using Dexter.Databases.EventTimers;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    
    public partial class CommunityCommands {

        /// <summary>
        /// Sends an event for admin approval (if <paramref name="EventType"/> is Official, it skips this step), and adds it to the database.
        /// </summary>
        /// <param name="EventType">The type of event hosting, either official or user-hosted.</param>
        /// <param name="Proposer">The User who proposed the event in the first place.</param>
        /// <param name="Release">The Time when the event is to be released to the public.</param>
        /// <param name="Description">The event description containing all relevant information as to partake in the event and what it consists on.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task AddEvent(EventType EventType, IGuildUser Proposer, DateTimeOffset Release, string Description) {
            if(Description.Length > 1000) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Event description too long!")
                    .WithDescription($"Try to keep the description under 1000 characters, the current description is {Description.Length} characters long.")
                    .SendEmbed(Context.Channel);
                return;
            }

            int ID = CommunityEventsDB.GenerateToken();
            EventStatus Status = EventType == EventType.UserHosted ? EventStatus.Pending : EventStatus.Approved;

            string ReleaseTimer = await CreateEventTimer(ReleaseEventCallback,
                new Dictionary<string, string> { { "ID", ID.ToString() } },
                (int) Release.Subtract(DateTimeOffset.Now).TotalSeconds,
                TimerType.Expire);

            string ProposalText = $"**New Event Suggestion >>>** {CommunityConfiguration.EventsNotificationMention}";

            string Link = Description.GetHyperLinks().FirstOrDefault();
            if (Link != null && Link.Length > 0) {
                ProposalText += $"\n{Link}";
            }

            ulong ProposalMessage = 0;
            if (EventType == EventType.UserHosted) { 
                IMessageChannel MessageChannel = DiscordSocketClient.GetChannel(CommunityConfiguration.EventsNotificationsChannel) as IMessageChannel;
                Embed EventInfo = CreateEventProposalEmbed(ID, Status, Proposer, Release.ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone)), Description).Build();
                IMessage Proposal = await MessageChannel.SendMessageAsync(
                    text: ProposalText,
                    embed: EventInfo);

                ProposalMessage = Proposal.Id;
            }

            CommunityEventsDB.Events.Add(
                new() {
                    ID = ID,
                    EventType = EventType,
                    ProposerID = Proposer.Id,
                    DateTimeProposed = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    DateTimeRelease = Release.ToUnixTimeSeconds(),
                    Description = Description,
                    Status = Status,
                    ResolveReason = "",
                    EventProposal = ProposalMessage,
                    ReleaseTimer = ReleaseTimer
                });

            CommunityEventsDB.SaveChanges();

            await UpdateEventProposal(ID);

            if (EventType == EventType.Official) {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Event #{ID} Successfully Programmed!")
                    .WithDescription($"The official server event has been successfully added to the database and will be displayed in <#{CommunityConfiguration.OfficialEventsChannel}> when its release time comes. \n" +
                        $"You can always check the information of this event with the command `{BotConfiguration.Prefix}events get id {ID}`")
                    .AddField("Release Time: ", $"{Release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z} ({Release.Humanize()})")
                    .SendEmbed(Context.Channel);
            } else {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Event #{ID} Successfully Suggested!")
                    .WithDescription($"Your suggestion went through! You will be informed when it is approved or declined. You can always check the status with `{BotConfiguration.Prefix}event get id {ID}`")
                    .AddField("Release Time: ", $"{Release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z} ({Release.Humanize()})")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
            }
        }

        /// <summary>
        /// A callback method to be called when an event suggestion is approved. It sets the status to approved and handles expired events.
        /// </summary>
        /// <param name="EventID">The unique numerical ID for the target event.</param>
        /// <param name="Reason">The optional reason for approval of the event.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task ApproveEventCallback(int EventID, string Reason = "") {
            CommunityEvent Event = GetEvent(EventID);
            IUser Proposer = DiscordSocketClient.GetUser(Event.ProposerID);

            if (Event == null) return;

            if (Event.Status == EventStatus.Expired) { 
                if (CommunityConfiguration.FailOnOverdueApproval) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("This Event has already expired!")
                        .WithDescription("Expired events can't be approved (FailOnOverdueApproval configuration is activated)")
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);
                    return;
                } else {
                    await BuildEmbed(EmojiEnum.Sign)
                        .WithTitle("This Event is overdue!")
                        .WithDescription("The event will be released immediately (FailOnOverDueApproval configuration is off)")
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);

                    Event.Status = EventStatus.Approved;
                    await ReleaseEvent(EventID);
                    return;
                }

            }

            DateTimeOffset Release = DateTimeOffset.FromUnixTimeSeconds(Event.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));

            if (!TimerService.TimerExists(Event.ReleaseTimer))
                Event.ReleaseTimer = await CreateEventTimer(ReleaseEventCallback,
                new Dictionary<string, string> { { "ID", Event.ID.ToString() } },
                (int)Release.Subtract(DateTimeOffset.Now).TotalSeconds,
                TimerType.Expire);

            Event.ResolveReason = Reason;
            Event.Status = EventStatus.Approved;
            await UpdateEventProposal(Event.ID);

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Event approved for release!")
                .WithDescription($"Event #{Event.ID} will be released at {Release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z} ({Release.Humanize()}).")
                .WithCurrentTimestamp()
                .SendDMAttachedEmbed(Context.Channel, BotConfiguration, Proposer, 
                    BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Your Event has been Approved!")
                    .WithDescription(Event.Description)
                    .AddField(Reason.Length > 0, "Reason: ", Reason)
                    .AddField("Release Time:", $"{Release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z}"));

            CommunityEventsDB.SaveChanges();
        }

        /// <summary>
        /// A callback method to be called when an event suggestion is declined. It sets the status to denied and handles timer cleanup.
        /// </summary>
        /// <param name="EventID">The unique numerical ID for the target event.</param>
        /// <param name="Reason">The optional reason why the event was declined.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task DeclineEventCallback(int EventID, string Reason = "") {
            CommunityEvent Event = GetEvent(EventID);
            IUser Proposer = DiscordSocketClient.GetUser(Event.ProposerID);

            if (Event == null) return;

            Event.ResolveReason = Reason;
            await UpdateEventProposal(Event.ID);

            if (TimerService.TimerExists(Event.ReleaseTimer))
                TimerService.RemoveTimer(Event.ReleaseTimer);

            await BuildEmbed(EmojiEnum.Annoyed)
                .WithTitle("Event has been declined!")
                .WithDescription($"Event #{Event.ID} has been declined.")
                .WithCurrentTimestamp()
                .SendDMAttachedEmbed(Context.Channel, BotConfiguration, Proposer, 
                    BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Your Event has been Declined!")
                    .WithDescription(Event.Description)
                    .AddField(Reason.Length > 0, "Reason: ", Reason));

            Event.Status = EventStatus.Denied;

            CommunityEventsDB.SaveChanges();
        }

        /// <summary>
        /// A callback event to be called when an event reaches its release. It sets the status to released if applicable and publishes the event to the appropriate channel.
        /// </summary>
        /// <param name="Args">A string-string Dictionary containing the field "ID", which must be parsable to a <c>ulong</c>.</param>

        public async Task ReleaseEventCallback(Dictionary<string, string> Args) {
            int EventID = int.Parse(Args["ID"]);

            await ReleaseEvent(EventID);
        }

        /// <summary>
        /// A function to be called when an event is released or updated after its release date.
        /// </summary>
        /// <param name="EventID">The unique numeric ID of the target event.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task ReleaseEvent(int EventID) {
            CommunityEvent Event = GetEvent(EventID);

            if (Event == null) return;

            if (Event.Status is EventStatus.Denied or EventStatus.Removed or EventStatus.Released) return;
            if (Event.Status == EventStatus.Expired && CommunityConfiguration.FailOnOverdueApproval) return;

            IUser User = DiscordSocketClient.GetUser(Event.ProposerID);

            if (Event.Status == EventStatus.Pending) {
                Event.Status = EventStatus.Expired;

                try {
                    await BuildEmbed(EmojiEnum.Sign)
                        .WithTitle("Event Expired")
                        .WithDescription($"One of your events expired without the admins giving their feedback in time! \n{Event.Description}")
                        .WithCurrentTimestamp()
                        .SendEmbed(await User.GetOrCreateDMChannelAsync());
                }
                catch (HttpException) { }
            }
            else {
                Event.Status = EventStatus.Released;

                ulong ChannelID = Event.EventType == EventType.Official ? CommunityConfiguration.OfficialEventsChannel : CommunityConfiguration.CommunityEventsChannel;
                IMessageChannel Channel = DiscordSocketClient.GetChannel(ChannelID) as IMessageChannel;

                string RoleMention = $"<@&{(Event.EventType == EventType.Official ? CommunityConfiguration.OfficialEventsNotifiedRole : CommunityConfiguration.CommunityEventsNotifiedRole)}>";
                string ProposalText = $"**New {(Event.EventType == EventType.Official ? "Official" : "Community")} Event >>>** {RoleMention} \n" +
                    $"*Event by <@{Event.ProposerID}>:* \n" +
                    $"{Event.Description}";

                await Channel.SendMessageAsync(text: ProposalText);
            }

            await UpdateEventProposal(Event.ID);

            CommunityEventsDB.SaveChanges();
        }

        /// <summary>
        /// Manually removes an event, automatically denying it and preventing it from running. It also deleted the associated proposal.
        /// </summary>
        /// <param name="EventID">The unique ID of the target Event.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task RemoveEvent(int EventID) {
            CommunityEvent Event = GetEvent(EventID);

            if (Event.Status == EventStatus.Released) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Event already released!")
                    .WithDescription("You can't remove an event that has already been released!")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
            }

            if (TimerService.TimerExists(Event.ReleaseTimer))
                TimerService.RemoveTimer(Event.ReleaseTimer);

            Event.Status = EventStatus.Removed;
            await UpdateEventProposal(Event.ID);

            CommunityEventsDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Event was successfully removed!")
                .WithDescription($"The event \"{Event.Description}\" has been removed from the proposal system.")
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Edits the target event to change its description to something new. It also deletes the old proposal and creates a new one with the updated information.
        /// </summary>
        /// <param name="EventID">The unique ID of the target event.</param>
        /// <param name="NewDescription">The new Description for the target event.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task EditEvent(int EventID, string NewDescription) {
            CommunityEvent Event = GetEvent(EventID);

            if (NewDescription.Length > 1000) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Event description too long!")
                    .WithDescription($"Try to keep the description under 1000 characters, the current description is {NewDescription.Length} characters long.")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (Event.EventType != EventType.Official) {
                if (Event.Status is EventStatus.Approved or EventStatus.Denied or EventStatus.Removed or EventStatus.Released) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Event is not pending!")
                        .WithDescription($"This event has already been {Event.Status}; and thus can't be edited. \nYou can remove this event and propose a new one if you wish to change it.")
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);
                    return;
                }
            }

            Event.Description = NewDescription;
            await UpdateEventProposal(Event.ID);

            CommunityEventsDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Event was successfully edited!")
                .WithDescription($"Event #{Event.ID}'s description has been changed to: \n" +
                    $"{Event.Description}")
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Resolves an event proposal by setting it as approved or declined depending on <paramref name="Action"/>.
        /// </summary>
        /// <param name="EventID">The unique numeric ID of the target event.</param>
        /// <param name="Reason">The optional reason behind the resolution.</param>
        /// <param name="Action">Either Action.Approve or Action.Decline, defines how the event proposal is resolved.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task ResolveEventProposal(int EventID, string Reason, Enums.ActionType Action) {
            CommunityEvent Event = GetEvent(EventID);

            if (Event == null) return;

            if (Event.Status is EventStatus.Approved or EventStatus.Denied or EventStatus.Released or EventStatus.Removed) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Event already resolved!")
                    .WithDescription($"Event #{EventID} has already been {Event.Status}.")
                    .SendEmbed(Context.Channel);
                return;
            }

            Event.Status = Action switch {
                Enums.ActionType.Approve => EventStatus.Approved,
                Enums.ActionType.Decline => EventStatus.Denied,
                _ => Event.Status
            };

            if (Action == Enums.ActionType.Approve) await ApproveEventCallback(EventID, Reason);
            else await DeclineEventCallback(EventID, Reason);

            Event.ResolveReason = Reason;
            await UpdateEventProposal(Event.ID);
        }

        /// <summary>
        /// Updates a proposal linked to a given ID, by setting its status and relevant information, modifying the linked embed.
        /// </summary>
        /// <param name="EventID">The ID of the target event to update.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task UpdateEventProposal(int EventID) {
            CommunityEvent Event = GetEvent(EventID);

            if (Event == null) return;

            if (Event.EventType == EventType.Official) return;

            SocketChannel ProposalChannel = DiscordSocketClient.GetChannel(CommunityConfiguration.EventsNotificationsChannel);

            IUserMessage Proposal = null;
            if (ProposalChannel is SocketTextChannel TextChannel)
                try {
                    Proposal = await TextChannel.GetMessageAsync(Event.EventProposal) as IUserMessage;
                }
                catch(HttpException) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Failed to update event proposal")
                        .WithDescription("The message doesn't exist anymore! Perhaps it was deleted?")
                        .AddField("Message ID", Event.EventProposal)
                        .SendEmbed(Context.Channel);
                }
            
            if (Proposal == null) return;

            string ProposalText = Event.Status switch {
                EventStatus.Pending => $"**New Event Proposal >>>** {CommunityConfiguration.EventsNotificationMention}",
                EventStatus.Approved => $"**Upcoming Event [{DateTimeOffset.FromUnixTimeSeconds(Event.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone)):MMM M 'at' h:mm tt}] >>>**",
                EventStatus.Released => $"**Released Event**",
                _ => $"**{Event.Status} Event Proposal >>>**"
            };

            if (Event.Status is EventStatus.Pending or EventStatus.Approved or EventStatus.Released) {
                string Link = Event.Description.GetHyperLinks().FirstOrDefault();
                if (Link != null && Link.Length > 0) {
                    ProposalText += $"\n{Link}";
                }
            }

            await Proposal.ModifyAsync(Properties => {
                Properties.Content = ProposalText;
                Properties.Embed = CreateEventProposalEmbed(Event).Build();
            });
        }

        private EmbedBuilder CreateEventProposalEmbed(int ID, EventStatus Status, IUser Author, DateTimeOffset Release, string Description, string ResolveReason = "") {
            bool IncludeResolutionInfo = CommunityConfiguration.IncludeEventResolutionInfo && (Status == EventStatus.Pending || (Status == EventStatus.Expired && !CommunityConfiguration.FailOnOverdueApproval));

            return BuildEmbed(EmojiEnum.Sign)
                .WithColor(new Color(uint.Parse(CommunityConfiguration.EventStatusColor[Status].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber)))
                .WithTitle(Status.ToString().ToUpper())
                .WithAuthor(Author)
                .WithDescription(Description)
                .AddField("Release Date:", $"{Release.ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone)):ddd', 'MMM d 'at' hh:mm tt 'UTC'z}")
                .AddField(IncludeResolutionInfo, "Resolution:", $"{BotConfiguration.Prefix}event [approve/decline] {ID}")
                .AddField(ResolveReason.Length > 0, "Reason:", ResolveReason)
                .WithFooter(ID.ToString())
                .WithCurrentTimestamp();
        }

        private EmbedBuilder CreateEventProposalEmbed(CommunityEvent Event) {
            IUser Author = DiscordSocketClient.GetUser(Event.ProposerID);
            DateTimeOffset Release = DateTimeOffset.FromUnixTimeSeconds(Event.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));

            return CreateEventProposalEmbed(Event.ID, Event.Status, Author, Release, Event.Description, Event.ResolveReason);
        }

        /// <summary>
        /// Gets an event from the events database given its <paramref name="EventID"/>.
        /// </summary>
        /// <param name="EventID">The ID of the target event.</param>
        /// <returns>An event whose ID is exactly that given by <paramref name="EventID"/>.</returns>

        public CommunityEvent GetEvent(int EventID) {
            CommunityEvent Event = CommunityEventsDB.Events.Find(EventID);

            return Event;
        }

        /// <summary>
        /// Gets an event from the events database given its <paramref name="Description"/>.
        /// </summary>
        /// <param name="Description">The text Description of the target event.</param>
        /// <returns>An event with the exact given <paramref name="Description"/>.</returns>

        public CommunityEvent GetEvent(string Description) {
            CommunityEvent Event = CommunityEventsDB.Events
                .AsQueryable()
                .Where(Event => Event.Description == Description)
                .FirstOrDefault();

            return Event;
        }

        /// <summary>
        /// Gets all events proposed by a given <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The user who proposed the target events.</param>
        /// <returns>An array of all events proposed by the <paramref name="User"/>.</returns>

        public CommunityEvent[] GetEvents(IUser User) {
            CommunityEvent[] Events = CommunityEventsDB.Events
                .AsQueryable()
                .Where(Event => Event.ProposerID == User.Id)
                .ToArray();

            return Events;
        }

        /// <summary>
        /// Generates an array of EmbedBuilders used for an EmbedMenu given a collection of events.
        /// </summary>
        /// <param name="Events">The collection of events to include in the final result.</param>
        /// <returns>An <c>EmbedBuilder[]</c> array containing paged embeds with each individual event in <paramref name="Events"/> as field entries.</returns>

        public EmbedBuilder[] GenerateUserEventsMenu(IEnumerable<CommunityEvent> Events) {
            if (!Events.Any()) return Array.Empty<EmbedBuilder>();

            int ExpectedPages = (Events.Count() + CommunityConfiguration.MaxEventsPerMenu - 1) / CommunityConfiguration.MaxEventsPerMenu; 

            EmbedBuilder[] Pages = new EmbedBuilder[ExpectedPages];
            IUser Author = DiscordSocketClient.GetUser(Events.First().ProposerID);

            int Page = 1;
            int Count = CommunityConfiguration.MaxEventsPerMenu;
            foreach(CommunityEvent e in Events) {
                if(++Count > CommunityConfiguration.MaxEventsPerMenu) {
                    Pages[Page - 1] = BuildEmbed(EmojiEnum.Sign)
                        .WithAuthor(Author)
                        .WithTitle($"Events - Page {Page}/{ExpectedPages}")
                        .WithFooter($"{Page++}/{ExpectedPages}");
                    Count = 1;
                }

                Pages[Page - 2].AddField(GenerateEventField(e));
            }

            return Pages.ToArray();
        }

        /// <summary>
        /// Creates a standardized field to display an event in an embed.
        /// </summary>
        /// <param name="Event">The event to take the field parameters from.</param>
        /// <returns>An EmbedFieldBuilder which can be used as an argument for <c>EmbedBuilder.AddField(FieldEmbedBuilder)</c>.</returns>

        public EmbedFieldBuilder GenerateEventField(CommunityEvent Event) {
            DateTimeOffset Release = DateTimeOffset.FromUnixTimeSeconds(Event.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));
            DateTimeOffset ProposeTime = DateTimeOffset.FromUnixTimeSeconds(Event.DateTimeProposed).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));

            string TimeInfo = Event.Status switch {
                EventStatus.Expired => $"**Proposed:** {ProposeTime:G} \n**Expired:** {Release:G}",
                EventStatus.Pending => $"**Proposed:** {ProposeTime:G} \n**Programmed for:** {Release:G}",
                EventStatus.Approved or EventStatus.Released => $"Release: {Release:G}",
                EventStatus.Removed or EventStatus.Denied or _ => $"**Proposed:** {ProposeTime:G}"
            };

            return new EmbedFieldBuilder()
                .WithName($"Event #{Event.ID} [**{Event.Status.ToString().ToUpper()}**]:")
                .WithValue($"{(Event.Description.Length > 256 ? Event.Description[..256] + "..." : Event.Description)}\n" +
                    TimeInfo);
        }

    }
}

using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.CommunityEvents;
using Dexter.Databases.EventTimers;
using Dexter.Databases.FunTopics;
using Dexter.Databases.Games;
using Dexter.Databases.UserProfiles;
using Dexter.Databases.UserRestrictions;
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

namespace Dexter.Commands
{

	/// <summary>
	/// The class containing all commands and utilities within the Community module.
	/// </summary>

	public partial class CommunityCommands : DiscordModule
	{

		/// <summary>
		/// Works as an interface between the configuration files attached to the Community module and its commands.
		/// </summary>

		public CommunityConfiguration CommunityConfiguration { get; set; }

		/// <summary>
		/// Includes important data used in parsing certain humanized terms like dates and times.
		/// </summary>

		public LanguageConfiguration LanguageConfiguration { get; set; }

		/// <summary>
		/// The moderation commands configuration containing data about the borkday role.
		/// </summary>

		public ModerationConfiguration ModerationConfiguration { get; set; }

		/// <summary>
		/// Loads the database containing events for the <c>~event</c> command.
		/// </summary>

		public CommunityEventsDB CommunityEventsDB { get; set; }

		/// <summary>
		/// Holds information about users who have been forbidden from using this service.
		/// </summary>

		public RestrictionsDB RestrictionsDB { get; set; }

		/// <summary>
		/// Holds all relevant data about games being played on Dexter.
		/// </summary>

		public GamesDB GamesDB { get; set; }

		/// <summary>
		/// Holds all relevant data about topics loaded into Dexter's database.
		/// </summary>

		public FunTopicsDB FunTopicsDB { get; set; }

		/// <summary>
		/// Holds all relevant data about user profiles.
		/// </summary>

		public ProfilesDB ProfilesDB { get; set; }

		/// <summary>
		/// Sends an event for admin approval (if <paramref name="eventType"/> is Official, it skips this step), and adds it to the database.
		/// </summary>
		/// <param name="eventType">The type of event hosting, either official or user-hosted.</param>
		/// <param name="proposer">The User who proposed the event in the first place.</param>
		/// <param name="release">The Time when the event is to be released to the public.</param>
		/// <param name="description">The event description containing all relevant information as to partake in the event and what it consists on.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public async Task AddEvent(EventType eventType, IGuildUser proposer, DateTimeOffset release, string description)
		{
			if (description.Length > 1000)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Event description too long!")
					.WithDescription($"Try to keep the description under 1000 characters, the current description is {description.Length} characters long.")
					.SendEmbed(Context.Channel);
				return;
			}

			int id = CommunityEventsDB.Events.Count() + 1;

			EventStatus status = eventType == EventType.UserHosted ? EventStatus.Pending : EventStatus.Approved;

			string releaseTimer = await CreateEventTimer(ReleaseEventCallback,
				new Dictionary<string, string> { { "ID", id.ToString() } },
				(int)release.Subtract(DateTimeOffset.Now).TotalSeconds,
				TimerType.Expire);

			string proposalText = $"**New Event Suggestion >>>** {CommunityConfiguration.EventsNotificationMention}";

			string link = description.GetHyperLinks().FirstOrDefault();
			if (link != null && link.Length > 0)
			{
				proposalText += $"\n{link}";
			}

			ulong proposalMessage = 0;

			if (eventType == EventType.UserHosted)
			{
				IMessageChannel MessageChannel = DiscordShardedClient.GetChannel(CommunityConfiguration.EventsNotificationsChannel) as IMessageChannel;
				Embed EventInfo = CreateEventProposalEmbed(id, status, proposer, release.ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone)), description).Build();
				IMessage Proposal = await MessageChannel.SendMessageAsync(
					text: proposalText,
					embed: EventInfo);

				proposalMessage = Proposal.Id;
			}

			CommunityEventsDB.Events.Add(
				new()
				{
					ID = id,
					EventType = eventType,
					ProposerID = proposer.Id,
					DateTimeProposed = DateTimeOffset.Now.ToUnixTimeSeconds(),
					DateTimeRelease = release.ToUnixTimeSeconds(),
					Description = description,
					Status = status,
					ResolveReason = "",
					EventProposal = proposalMessage,
					ReleaseTimer = releaseTimer
				});

			await UpdateEventProposal(id);

			if (eventType == EventType.Official)
			{
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle($"Event #{id} Successfully Programmed!")
					.WithDescription($"The official server event has been successfully added to the database and will be displayed in <#{CommunityConfiguration.OfficialEventsChannel}> when its release time comes. \n" +
						$"You can always check the information of this event with the command `{BotConfiguration.Prefix}events get id {id}`")
					.AddField("Release Time: ", $"{release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z} ({release.Humanize()})")
					.SendEmbed(Context.Channel);
			}
			else
			{
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle($"Event #{id} Successfully Suggested!")
					.WithDescription($"Your suggestion went through! You will be informed when it is approved or declined. You can always check the status with `{BotConfiguration.Prefix}event get id {id}`")
					.AddField("Release Time: ", $"{release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z} ({release.Humanize()})")

					.SendEmbed(Context.Channel);
			}
		}

		/// <summary>
		/// A callback method to be called when an event suggestion is approved. It sets the status to approved and handles expired events.
		/// </summary>
		/// <param name="eventID">The unique numerical ID for the target event.</param>
		/// <param name="reason">The optional reason for approval of the event.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public async Task ApproveEventCallback(int eventID, string reason = "")
		{
			CommunityEvent cEvent = GetEvent(eventID);
			IUser proposer = DiscordShardedClient.GetUser(cEvent.ProposerID);

			if (cEvent == null) return;

			if (cEvent.Status == EventStatus.Expired)
			{
				if (CommunityConfiguration.FailOnOverdueApproval)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("This Event has already expired!")
						.WithDescription("Expired events can't be approved (FailOnOverdueApproval configuration is activated)")

						.SendEmbed(Context.Channel);
					return;
				}
				else
				{
					await BuildEmbed(EmojiEnum.Sign)
						.WithTitle("This Event is overdue!")
						.WithDescription("The event will be released immediately (FailOnOverDueApproval configuration is off)")

						.SendEmbed(Context.Channel);

					cEvent.Status = EventStatus.Approved;
					await ReleaseEvent(eventID);
					return;
				}

			}

			DateTimeOffset release = DateTimeOffset.FromUnixTimeSeconds(cEvent.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));

			if (!TimerService.TimerExists(cEvent.ReleaseTimer))
				cEvent.ReleaseTimer = await CreateEventTimer(ReleaseEventCallback,
				new Dictionary<string, string> { { "ID", cEvent.ID.ToString() } },
				(int)release.Subtract(DateTimeOffset.Now).TotalSeconds,
				TimerType.Expire);

			cEvent.ResolveReason = reason;
			cEvent.Status = EventStatus.Approved;
			await UpdateEventProposal(cEvent.ID);

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Event approved for release!")
				.WithDescription($"Event #{cEvent.ID} will be released at {release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z} ({release.Humanize()}).")
				.SendDMAttachedEmbed(Context.Channel, BotConfiguration, proposer,
					BuildEmbed(EmojiEnum.Love)
					.WithTitle("Your Event has been Approved!")
					.WithDescription(cEvent.Description)
					.AddField(reason.Length > 0, "Reason: ", reason)
					.AddField("Release Time:", $"{release:ddd', 'MMM d 'at' hh:mm tt 'UTC'z}"));
		}

		/// <summary>
		/// A callback method to be called when an event suggestion is declined. It sets the status to denied and handles timer cleanup.
		/// </summary>
		/// <param name="eventID">The unique numerical ID for the target event.</param>
		/// <param name="reason">The optional reason why the event was declined.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public async Task DeclineEventCallback(int eventID, string reason = "")
		{
			CommunityEvent cEvent = GetEvent(eventID);
			IUser proposer = DiscordShardedClient.GetUser(cEvent.ProposerID);

			if (cEvent == null) return;

			cEvent.ResolveReason = reason;
			await UpdateEventProposal(cEvent.ID);

			if (TimerService.TimerExists(cEvent.ReleaseTimer))
				await TimerService.RemoveTimer(cEvent.ReleaseTimer);

			await BuildEmbed(EmojiEnum.Annoyed)
				.WithTitle("Event has been declined!")
				.WithDescription($"Event #{cEvent.ID} has been declined.")
				.SendDMAttachedEmbed(Context.Channel, BotConfiguration, proposer,
					BuildEmbed(EmojiEnum.Love)
					.WithTitle("Your Event has been Declined!")
					.WithDescription(cEvent.Description)
					.AddField(reason.Length > 0, "Reason: ", reason));

			cEvent.Status = EventStatus.Denied;
		}

		/// <summary>
		/// A callback event to be called when an event reaches its release. It sets the status to released if applicable and publishes the event to the appropriate channel.
		/// </summary>
		/// <param name="args">A string-string Dictionary containing the field "ID", which must be parsable to a <c>ulong</c>.</param>

		public async Task ReleaseEventCallback(Dictionary<string, string> args)
		{
			int eventID = int.Parse(args["ID"]);

			await ReleaseEvent(eventID);
		}

		/// <summary>
		/// A function to be called when an event is released or updated after its release date.
		/// </summary>
		/// <param name="eventID">The unique numeric ID of the target event.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public async Task ReleaseEvent(int eventID)
		{
			CommunityEvent cEvent = GetEvent(eventID);

			if (cEvent == null) return;

			if (cEvent.Status is EventStatus.Denied or EventStatus.Removed or EventStatus.Released) return;
			if (cEvent.Status == EventStatus.Expired && CommunityConfiguration.FailOnOverdueApproval) return;

			IUser user = DiscordShardedClient.GetUser(cEvent.ProposerID);

			if (cEvent.Status == EventStatus.Pending)
			{
				cEvent.Status = EventStatus.Expired;

				try
				{
					await BuildEmbed(EmojiEnum.Sign)
						.WithTitle("Event Expired")
						.WithDescription($"One of your events expired without the admins giving their feedback in time! \n{cEvent.Description}")

						.SendEmbed(await user.CreateDMChannelAsync());
				}
				catch (HttpException) { }
			}
			else
			{
				cEvent.Status = EventStatus.Released;

				ulong channelID = cEvent.EventType == EventType.Official ? CommunityConfiguration.OfficialEventsChannel : CommunityConfiguration.CommunityEventsChannel;
				IMessageChannel channel = DiscordShardedClient.GetChannel(channelID) as IMessageChannel;

				string roleMention = $"<@&{(cEvent.EventType == EventType.Official ? CommunityConfiguration.OfficialEventsNotifiedRole : CommunityConfiguration.CommunityEventsNotifiedRole)}>";
				string proposalText = $"**New {(cEvent.EventType == EventType.Official ? "Official" : "Community")} Event >>>** {roleMention} \n" +
					$"*Event by <@{cEvent.ProposerID}>:* \n" +
					$"{cEvent.Description}";

				await channel.SendMessageAsync(text: proposalText);
			}

			await UpdateEventProposal(cEvent.ID);
		}

		/// <summary>
		/// Manually removes an event, automatically denying it and preventing it from running. It also deleted the associated proposal.
		/// </summary>
		/// <param name="eventID">The unique ID of the target Event.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public async Task RemoveEvent(int eventID)
		{
			CommunityEvent cEvent = GetEvent(eventID);

			if (cEvent.Status == EventStatus.Released)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Event already released!")
					.WithDescription("You can't remove an event that has already been released!")

					.SendEmbed(Context.Channel);
			}

			if (TimerService.TimerExists(cEvent.ReleaseTimer))
				await TimerService.RemoveTimer(cEvent.ReleaseTimer);

			cEvent.Status = EventStatus.Removed;
			await UpdateEventProposal(cEvent.ID);

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Event was successfully removed!")
				.WithDescription($"The event \"{cEvent.Description}\" has been removed from the proposal system.")
				.SendEmbed(Context.Channel);
		}

		/// <summary>
		/// Edits the target event to change its description to something new. It also deletes the old proposal and creates a new one with the updated information.
		/// </summary>
		/// <param name="eventID">The unique ID of the target event.</param>
		/// <param name="newDescription">The new Description for the target event.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public async Task EditEvent(int eventID, string newDescription)
		{
			CommunityEvent cEvent = GetEvent(eventID);

			if (newDescription.Length > 1000)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Event description too long!")
					.WithDescription($"Try to keep the description under 1000 characters, the current description is {newDescription.Length} characters long.")
					.SendEmbed(Context.Channel);
				return;
			}

			if (cEvent.EventType != EventType.Official)
			{
				if (cEvent.Status is EventStatus.Approved or EventStatus.Denied or EventStatus.Removed or EventStatus.Released)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Event is not pending!")
						.WithDescription($"This event has already been {cEvent.Status}; and thus can't be edited. \nYou can remove this event and propose a new one if you wish to change it.")

						.SendEmbed(Context.Channel);
					return;
				}
			}

			cEvent.Description = newDescription;
			await UpdateEventProposal(cEvent.ID);

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Event was successfully edited!")
				.WithDescription($"Event #{cEvent.ID}'s description has been changed to: \n" +
					$"{cEvent.Description}")
				.SendEmbed(Context.Channel);
		}

		/// <summary>
		/// Resolves an event proposal by setting it as approved or declined depending on <paramref name="action"/>.
		/// </summary>
		/// <param name="eventID">The unique numeric ID of the target event.</param>
		/// <param name="reason">The optional reason behind the resolution.</param>
		/// <param name="action">Either Action.Approve or Action.Decline, defines how the event proposal is resolved.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public async Task ResolveEventProposal(int eventID, string reason, Enums.ActionType action)
		{
			CommunityEvent cEvent = GetEvent(eventID);

			if (cEvent == null) return;

			if (cEvent.Status is EventStatus.Approved or EventStatus.Denied or EventStatus.Released or EventStatus.Removed)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Event already resolved!")
					.WithDescription($"Event #{eventID} has already been {cEvent.Status}.")
					.SendEmbed(Context.Channel);
				return;
			}

			cEvent.Status = action switch
			{
				Enums.ActionType.Approve => EventStatus.Approved,
				Enums.ActionType.Decline => EventStatus.Denied,
				_ => cEvent.Status
			};

			if (action == Enums.ActionType.Approve) await ApproveEventCallback(eventID, reason);
			else await DeclineEventCallback(eventID, reason);

			cEvent.ResolveReason = reason;
			await UpdateEventProposal(cEvent.ID);
		}

		/// <summary>
		/// Updates a proposal linked to a given ID, by setting its status and relevant information, modifying the linked embed.
		/// </summary>
		/// <param name="eventID">The ID of the target event to update.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public async Task UpdateEventProposal(int eventID)
		{
			CommunityEvent cEvent = GetEvent(eventID);

			if (cEvent == null) return;

			if (cEvent.EventType == EventType.Official) return;

			SocketChannel proposalChannel = DiscordShardedClient.GetChannel(CommunityConfiguration.EventsNotificationsChannel);

			IUserMessage proposal = null;

			if (proposalChannel is SocketTextChannel textChannel)
				try
				{
					proposal = await textChannel.GetMessageAsync(cEvent.EventProposal) as IUserMessage;
				}
				catch (HttpException)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Failed to update event proposal")
						.WithDescription("The message doesn't exist anymore! Perhaps it was deleted?")
						.AddField("Message ID", cEvent.EventProposal)
						.SendEmbed(Context.Channel);
				}

			if (proposal == null) return;

			string proposalText = cEvent.Status switch
			{
				EventStatus.Pending => $"**New Event Proposal >>>** {CommunityConfiguration.EventsNotificationMention}",
				EventStatus.Approved => $"**Upcoming Event [{DateTimeOffset.FromUnixTimeSeconds(cEvent.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone)):MMM M 'at' h:mm tt}] >>>**",
				EventStatus.Released => $"**Released Event**",
				_ => $"**{cEvent.Status} Event Proposal >>>**"
			};

			if (cEvent.Status is EventStatus.Pending or EventStatus.Approved or EventStatus.Released)
			{
				string link = cEvent.Description.GetHyperLinks().FirstOrDefault();

				if (link != null && link.Length > 0)
					proposalText += $"\n{link}";
			}

			await proposal.ModifyAsync(Pproperties =>
			{
				Pproperties.Content = proposalText;
				Pproperties.Embed = CreateEventProposalEmbed(cEvent).Build();
			});
		}

		private EmbedBuilder CreateEventProposalEmbed(int id, EventStatus status, IUser author, DateTimeOffset release, string description, string resolveReason = "")
		{
			bool includeResolutionInfo = CommunityConfiguration.IncludeEventResolutionInfo && (status == EventStatus.Pending || (status == EventStatus.Expired && !CommunityConfiguration.FailOnOverdueApproval));

			return BuildEmbed(EmojiEnum.Sign)
				.WithColor(new Color(uint.Parse(CommunityConfiguration.EventStatusColor[status].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber)))
				.WithTitle(status.ToString().ToUpper())
				.WithAuthor(author)
				.WithDescription(description)
				.AddField("Release Date:", $"{release.ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone)):ddd', 'MMM d 'at' hh:mm tt 'UTC'z}")
				.AddField(status == EventStatus.Pending, "Approval/Decline:", $"`{BotConfiguration.Prefix}event <approve|decline> {id}`")
				.AddField(includeResolutionInfo, "Resolution:", $"{BotConfiguration.Prefix}event [approve/decline] {id}")
				.AddField(resolveReason.Length > 0, "Reason:", resolveReason)
				.WithFooter(id.ToString());
		}

		private EmbedBuilder CreateEventProposalEmbed(CommunityEvent cEvent)
		{
			IUser author = DiscordShardedClient.GetUser(cEvent.ProposerID);
			DateTimeOffset release = DateTimeOffset.FromUnixTimeSeconds(cEvent.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));

			return CreateEventProposalEmbed(cEvent.ID, cEvent.Status, author, release, cEvent.Description, cEvent.ResolveReason);
		}

		/// <summary>
		/// Gets an event from the events database given its <paramref name="eventID"/>.
		/// </summary>
		/// <param name="eventID">The ID of the target event.</param>
		/// <returns>An event whose ID is exactly that given by <paramref name="eventID"/>.</returns>

		public CommunityEvent GetEvent(int eventID)
		{
			return CommunityEventsDB.Events.Find(eventID);
		}

		/// <summary>
		/// Gets an event from the events database given its <paramref name="description"/>.
		/// </summary>
		/// <param name="description">The text Description of the target event.</param>
		/// <returns>An event with the exact given <paramref name="description"/>.</returns>

		public CommunityEvent GetEvent(string description)
		{
			return CommunityEventsDB.Events
				.AsQueryable()
				.Where(Event => Event.Description == description)
				.FirstOrDefault();
		}

		/// <summary>
		/// Gets all events proposed by a given <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user who proposed the target events.</param>
		/// <returns>An array of all events proposed by the <paramref name="user"/>.</returns>

		public CommunityEvent[] GetEvents(IUser user)
		{
			return CommunityEventsDB.Events
				.AsQueryable()
				.Where(Event => Event.ProposerID == user.Id)
				.ToArray();
		}

		/// <summary>
		/// Generates an array of EmbedBuilders used for an EmbedMenu given a collection of events.
		/// </summary>
		/// <param name="events">The collection of events to include in the final result.</param>
		/// <returns>An <c>EmbedBuilder[]</c> array containing paged embeds with each individual event in <paramref name="events"/> as field entries.</returns>

		public EmbedBuilder[] GenerateUserEventsMenu(IEnumerable<CommunityEvent> events)
		{
			if (!events.Any()) return Array.Empty<EmbedBuilder>();

			int expectedPages = (events.Count() + CommunityConfiguration.MaxEventsPerMenu - 1) / CommunityConfiguration.MaxEventsPerMenu;

			EmbedBuilder[] pages = new EmbedBuilder[expectedPages];
			IUser author = DiscordShardedClient.GetUser(events.First().ProposerID);

			int page = 1;
			int count = CommunityConfiguration.MaxEventsPerMenu;

			foreach (CommunityEvent e in events)
			{
				if (++count > CommunityConfiguration.MaxEventsPerMenu)
				{
					pages[page - 1] = BuildEmbed(EmojiEnum.Sign)
						.WithAuthor(author)
						.WithTitle($"Events");
					count = 1;
				}

				pages[page - 2].AddField(GenerateEventField(e));
			}

			return pages.ToArray();
		}

		/// <summary>
		/// Creates a standardized field to display an event in an embed.
		/// </summary>
		/// <param name="cEvent">The event to take the field parameters from.</param>
		/// <returns>An EmbedFieldBuilder which can be used as an argument for <c>EmbedBuilder.AddField(FieldEmbedBuilder)</c>.</returns>

		public EmbedFieldBuilder GenerateEventField(CommunityEvent cEvent)
		{
			DateTimeOffset release = DateTimeOffset.FromUnixTimeSeconds(cEvent.DateTimeRelease).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));
			DateTimeOffset proposeTime = DateTimeOffset.FromUnixTimeSeconds(cEvent.DateTimeProposed).ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));

			string timeInfo = cEvent.Status switch
			{
				EventStatus.Expired => $"**Proposed:** {proposeTime:G} \n**Expired:** {release:G}",
				EventStatus.Pending => $"**Proposed:** {proposeTime:G} \n**Programmed for:** {release:G}",
				EventStatus.Approved or EventStatus.Released => $"Release: {release:G}",
				EventStatus.Removed or EventStatus.Denied or _ => $"**Proposed:** {proposeTime:G}"
			};

			return new EmbedFieldBuilder()
				.WithName($"Event #{cEvent.ID} [**{cEvent.Status.ToString().ToUpper()}**]:")
				.WithValue($"{(cEvent.Description.Length > 256 ? cEvent.Description[..256] + "..." : cEvent.Description)}\n" +
					timeInfo);
		}

	}

}

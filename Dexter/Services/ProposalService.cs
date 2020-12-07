using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Databases.Proposals;
using Discord.Net;
using Dexter.Databases.AdminConfirmations;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Net;
using System.IO;

namespace Dexter.Services {

    /// <summary>
    /// The Proposal service, which is used to create and update proposals on the change of a reaction.
    /// </summary>
    
    public class ProposalService : Service {

        /// <summary>
        /// The ServiceProvider is used to run the initialized command and method that an admin confirmation will call back to.
        /// </summary>
        public ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The ProposalConfiguration, which contains the location of the emoji storage guild, as well as IDs of channels.
        /// </summary>

        public ProposalConfiguration ProposalConfiguration { get; set; }

        /// <summary>
        /// The ProposalDB is used as a storage for the proposals.
        /// </summary>
        
        public ProposalDB ProposalDB { get; set; }

        /// <summary>
        /// The Random instance is used to pick a set number of random characters from the configuration to create a token.
        /// </summary>
        
        public Random Random { get; set; }

        /// <summary>
        /// The Initialize method hooks the client MessageReceived, ReactionAdded and ReactionRemoved events and sets them to their related delegates.
        /// </summary>

        public override void Initialize() {
            DiscordSocketClient.MessageReceived += MessageRecieved;
            DiscordSocketClient.ReactionAdded += ReactionAdded;
            DiscordSocketClient.ReactionRemoved += ReactionRemoved;
        }

        /// <summary>
        /// The EditSuggestion method is used to approve/decline a proposed subject. To achieve this, it takes in the related fields and applies them to the given context.
        /// </summary>
        /// <param name="Tracker">The tracker of the proposal, used to search up the suggestion in the given database.</param>
        /// <param name="Reason">The optional reason for the proposal having been approved / declined.</param>
        /// <param name="Approver">The administrator who has approved the given suggestion.</param>
        /// <param name="MessageChannel">The channel in which the approval command was run.</param>
        /// <param name="ProposalStatus">The type of proposal status you wish to set the embed to.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task EditProposal(string Tracker, string Reason, IUser Approver, IMessageChannel MessageChannel, ProposalStatus ProposalStatus) {
            Proposal Proposal = ProposalDB.GetProposalByNameOrID(Tracker);

            if (Proposal == null)
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Proposal does not exist!")
                    .WithDescription($"Cound not fetch the proposal from tracker / message ID / staff message ID `{Tracker}`.\n" +
                        $"Are you sure it exists?")
                    .SendEmbed(MessageChannel);
            else {
                Proposal.Reason = Reason;

                await UpdateProposal(Proposal, ProposalStatus);

                EmbedBuilder Builder = BuildEmbed(ProposalStatus == ProposalStatus.Declined ? EmojiEnum.Annoyed : EmojiEnum.Love)
                    .WithTitle($"{Proposal.ProposalType.ToString().Prettify()} {Proposal.ProposalStatus}.")
                    .WithDescription($"The {Proposal.ProposalType.ToString().Prettify().ToLower()} `{Proposal.Tracker}` was successfully {Proposal.ProposalStatus.ToString().ToLower()} by {Approver.Mention}.")
                    .AddField("Reason", string.IsNullOrEmpty(Reason) ? "No reason provided" : Reason)
                    .WithCurrentTimestamp();

                try {
                    await BuildProposal(Proposal).SendEmbed(await DiscordSocketClient.GetUser(Proposal.Proposer).GetOrCreateDMChannelAsync());

                    Builder.AddField("Success", "The DM was successfully sent!");
                } catch (HttpException) {
                    Builder.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
                }

                await Builder.SendEmbed(MessageChannel);
            }
        }

        /// <summary>
        /// The ReactionAdded method checks to see if reaction is added in the suggestion channel and, if so, it runs CheckAsync.
        /// </summary>
        /// <param name="CachedMessage">The cached message the reaction was applied to, used to get the suggestion it relates to.</param>
        /// <param name="MessageChannel">The channel of which the reaction was added to.</param>
        /// <param name="Reaction">The reaction of question which was added.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel MessageChannel, SocketReaction Reaction) {
            if (MessageChannel.Id != ProposalConfiguration.SuggestionsChannel || Reaction.User.Value.IsBot)
                return;

            IUserMessage Message = await CachedMessage.GetOrDownloadAsync();

            if (Message == null)
                throw new Exception("Message does not exist in cache and could not be downloaded! Aborting...");

            if (await CheckAsync(Message, Reaction))
                await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
        }

        /// <summary>
        /// The ReactionRemoved method checks to see if reaction was removed in the suggestion channel and, if so, it runs CheckAsync.
        /// </summary>
        /// <param name="CachedMessage">The cached message the reaction was removed from, used to get the suggestion it relates to.</param>
        /// <param name="MessageChannel">The channel of which the reaction was removed from.</param>
        /// <param name="Reaction">The reaction of question which was removed.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel MessageChannel, SocketReaction Reaction) {
            if (MessageChannel.Id != ProposalConfiguration.SuggestionsChannel || Reaction.User.Value.IsBot)
                return;

            IUserMessage Message = await CachedMessage.GetOrDownloadAsync();

            if (Message == null)
                throw new Exception("Message does not exist in cache and could not be downloaded! Aborting...");

            await CheckAsync(Message, Reaction);
        }

        /// <summary>
        /// The MessageRecieved method runs when a message is sent in the suggestions channel, and runs checks to see if the message is a suggestion.
        /// </summary>
        /// <param name="RecievedMessage">A SocketMessage object, which contains details about the message such as its content and attachments.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public Task MessageRecieved(SocketMessage RecievedMessage) {
            // Check to see if the message has been sent in the suggestion channel and is not a bot, least we return.
            if (RecievedMessage.Channel.Id != ProposalConfiguration.SuggestionsChannel || RecievedMessage.Author.IsBot)
                return Task.CompletedTask;

            _ = Task.Run(async () => await CreateSuggestion(RecievedMessage));

            return Task.CompletedTask;
        }

        /// <summary>
        /// The CreateSuggestion method converts the message to a proposal and suggestion object.
        /// This suggestion object is then sent back to the channel once deleted as a formatted embed for use to vote on.
        /// </summary>
        /// <param name="RecievedMessage">A SocketMessage object, which contains details about the message such as its content and attachments.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        public async Task CreateSuggestion (SocketMessage RecievedMessage) {
            // Check to see if the embed message length is more than 1750, else will fail the embed from sending due to character limits.
            if (RecievedMessage.Content.Length > 1750) {
                await RecievedMessage.DeleteAsync();

                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Your suggestion is too big!")
                    .WithDescription("Please try to summarise your suggestion a little! " +
                    "Keep in mind that emoji add a lot of characters to your suggestion - even if it doesn't seem like it - " +
                    "as Discord handles emoji differently to text, so if you're using a lot of emoji try to cut down on those! <3")
                    .SendEmbed(RecievedMessage.Author, RecievedMessage.Channel as ITextChannel);
                return;
            }

            // Creates a new Proposal object with the related fields.
            Proposal Proposal = new() {
                Tracker = CreateToken(),
                Content = RecievedMessage.Content,
                ProposalStatus = ProposalStatus.Suggested,
                Proposer = RecievedMessage.Author.Id,
                ProposalType = ProposalType.Suggestion
            };

            Attachment Attachment = RecievedMessage.Attachments.FirstOrDefault();

            if (Attachment != null) {
                string TemporaryLogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");

                if (!Directory.Exists(TemporaryLogDirectory))
                    Directory.CreateDirectory(TemporaryLogDirectory);

                string FilePath = Path.Combine(TemporaryLogDirectory, $"{Proposal.Tracker}.{Path.GetExtension(Attachment.Filename)}");

                using WebClient WebClient = new();

                await WebClient.DownloadFileTaskAsync(Attachment.ProxyUrl, FilePath);

                ITextChannel Channel = DiscordSocketClient.GetChannel(ProposalConfiguration.PictureChannel) as ITextChannel;

                IUserMessage AttachmentMSG = await Channel.SendFileAsync(FilePath);

                Proposal.ProxyURL = AttachmentMSG.Attachments.FirstOrDefault().ProxyUrl;

                File.Delete(FilePath);
            }

            // Creates a new Suggestion object with the related fields.
            Suggestion Suggested = new () {
                Tracker = Proposal.Tracker
            };

            RestUserMessage Embed = await RecievedMessage.Channel.SendMessageAsync(embed: BuildProposal(Proposal).Build());

            // Delete the message sent by the user.
            await RecievedMessage.DeleteAsync();

            // Set the message ID in the suggestion object to the ID of the embed.
            Proposal.MessageID = Embed.Id;

            // Add the suggestion and proposal objects to the database.
            ProposalDB.Proposals.Add(Proposal);
            ProposalDB.Suggestions.Add(Suggested);

            await ProposalDB.SaveChangesAsync();

            // Add the related emoji specified in the ProposalConfiguration to the suggestion.
            SocketGuild Guild = DiscordSocketClient.GetGuild(ProposalConfiguration.StorageGuild);

            foreach (string Emoji in ProposalConfiguration.SuggestionEmoji) {
                GuildEmote Emote = await Guild.GetEmoteAsync(ProposalConfiguration.Emoji[Emoji]);
                await Embed.AddReactionAsync(Emote);
            }
        }

        /// <summary>
        /// The SendAdminConfirmation method runs when a function is needed for admin confirmation to run.
        /// </summary>
        /// <param name="JSON">The JSON is the parameters that will be called back to the method containing a dictionary.</param>
        /// <param name="Type">The type is the class the method will call back to when it has been approved.</param>
        /// <param name="Method">The method is the method of which will be called once this command has been run.</param>
        /// <param name="Author">The author is the snowflake ID of the user who has suggested the proposal.</param>
        /// <param name="ProposedMessage">The proposed message is the content of the approval confirmation message.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task SendAdminConfirmation(string JSON, string Type, string Method, ulong Author, string ProposedMessage) {
            // Creates a new Proposal object with the related fields.
            Proposal Proposal = new() {
                Tracker = CreateToken(),
                Content = ProposedMessage,
                ProposalStatus = ProposalStatus.Suggested,
                ProposalType = ProposalType.AdminConfirmation,
                Proposer = Author
            };

            // Creates a new AdminConfirmation object with the related fields.
            AdminConfirmation Confirmation = new() {
                Tracker = Proposal.Tracker,
                CallbackClass = Type,
                CallbackMethod = Method,
                CallbackParameters = JSON
            };

            RestUserMessage Embed = await (DiscordSocketClient.GetChannel(BotConfiguration.ModerationLogChannelID) as SocketTextChannel).SendMessageAsync(embed: BuildProposal(Proposal).Build());

            // Set the message ID in the suggestion object to the ID of the embed.
            Proposal.MessageID = Embed.Id;

            // Add the confirmation and proposal objects to the database.
            ProposalDB.Proposals.Add(Proposal);
            ProposalDB.AdminConfirmations.Add(Confirmation);

            await ProposalDB.SaveChangesAsync();
        }

        /// <summary>
        /// The Check Async method checks to see whether or not a suggestion is passing or has been denied in the suggestions channel.
        /// It also handles the removing of the embed through the trash can emoji.
        /// </summary>
        /// <param name="UserMessage">The message that has had the emoji applied or removed from it.</param>
        /// <param name="Reaction">The reaction that was applied.</param>
        /// <returns>A boolean value of whether or not you should remove the emoji from after the method has run.</returns>
        
        public async Task<bool> CheckAsync(IUserMessage UserMessage, SocketReaction Reaction) {
            // Get the suggestion from the database which has a message ID which matches the one of which we're looking for.
            Proposal Proposal = ProposalDB.Proposals.AsQueryable().Where(Suggestion => Suggestion.MessageID == UserMessage.Id).FirstOrDefault();

            // Check if the proposal is not null.
            if (Proposal == null)
                throw new Exception("Proposal does not exist in database! Aborting...");

            if (Proposal.ProposalStatus != ProposalStatus.Suggested)
                return true;

            // Check the current amount of upvotes the message has.
            ReactionMetadata Upvotes = UserMessage.Reactions[
                await DiscordSocketClient.GetGuild(ProposalConfiguration.StorageGuild)
                .GetEmoteAsync(ProposalConfiguration.Emoji["Upvote"])
            ];

            // Check the current amount of downvotes the message has.
            ReactionMetadata Downvotes = UserMessage.Reactions[
                await DiscordSocketClient.GetGuild(ProposalConfiguration.StorageGuild)
                .GetEmoteAsync(ProposalConfiguration.Emoji["Downvote"])
            ];

            // Check if the suggestion has enough upvotes to trigger the suggestion to pass,
            // or if the suggestion has enough downvotes to force it to deny itself.
            switch (CheckVotes(Upvotes, Downvotes)) {
                case SuggestionVotes.Pass:
                    // Set the suggestion to pending if it has passed.
                    await UpdateProposal(Proposal, ProposalStatus.Pending);

                    // Create a new embed in the staff suggestions channel of the current suggestion.
                    RestUserMessage StaffSuggestion = await (UserMessage.Channel as SocketGuildChannel).Guild
                        .GetTextChannel(ProposalConfiguration.StaffSuggestionsChannel)
                        .SendMessageAsync(embed: BuildProposal(Proposal).Build());

                    Suggestion Suggestion = ProposalDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.Tracker == Proposal.Tracker).FirstOrDefault();

                    // Check if the suggestion is not null.
                    if (Suggestion == null)
                        throw new Exception("Suggestion does not exist in database! Aborting...");

                    // Set the staff message ID in the suggestions database to the new suggestion.
                    Suggestion.StaffMessageID = StaffSuggestion.Id;
                    await ProposalDB.SaveChangesAsync();

                    // Get the staff suggestions channel and add the related emoji to the message.
                    SocketGuild EmojiCacheGuild = DiscordSocketClient.GetGuild(ProposalConfiguration.StorageGuild);

                    foreach (string Emoji in ProposalConfiguration.StaffSuggestionEmoji) {
                        GuildEmote EmoteStaff = await EmojiCacheGuild.GetEmoteAsync(ProposalConfiguration.Emoji[Emoji]);
                        await StaffSuggestion.AddReactionAsync(EmoteStaff);
                    }

                    break;
                case SuggestionVotes.Fail:
                    // If the suggestion has been declined by the community, set the reason of the suggestion to be
                    // "Declined by the community." and update the suggestion to a status of having been declined.
                    Proposal.Reason = "Declined by the community.";
                    await UpdateProposal(Proposal, ProposalStatus.Declined);
                    break;
                case SuggestionVotes.Remain:
                    break;
            }

            // Run through the emote that has been applied to the suggestions channel to see if the ID of the emote matches one of our designated suggestion emoji. 
            if (Reaction.Emote is Emote Emote)
                foreach (KeyValuePair<string, ulong> Emotes in ProposalConfiguration.Emoji)
                    if (Emotes.Value == Emote.Id) {
                        switch (Emotes.Key) {
                            case "Upvote":
                            case "Downvote":
                                // If an upvote or downvote has been applied by the suggestor, remove it.
                                if (Proposal.Proposer == Reaction.UserId || (Reaction.User.Value as IGuildUser).GetPermissionLevel(BotConfiguration) >= PermissionLevel.Moderator)
                                    return true;
                                else
                                    return false;
                            case "Bin":
                                // If the suggestion has had the bin icon applied by the user, set the status of the suggestion to "deleted" and delete the message.
                                if (Proposal.Proposer == Reaction.UserId || (Reaction.User.Value as IGuildUser).GetPermissionLevel(BotConfiguration) >= PermissionLevel.Moderator) {
                                    await UpdateProposal(Proposal, ProposalStatus.Deleted);
                                    await UserMessage.DeleteAsync();
                                    return false;
                                }
                                break;
                            default:
                                // If the suggestion emoji does exist in our configuration but is not specified by a name above there has been a mismatch and it will error respectively.
                                throw new Exception("Unknown reaction exists in JSON config but is not assigned the correct name! " +
                                    $"Please make sure that the reaction {Emotes.Key} is assigned the correct value in {GetType().Name}.");
                        }
                    }
            
            // Remove the emoji if it is not one that affects the suggestion.
            return true;
        }

        /// <summary>
        /// The CheckVotes method checks to see if the difference in reactions triggers a change in the suggestion.
        /// </summary>
        /// <param name="Upvotes">The metadata containing the amount of upvotes the suggestion has.</param>
        /// <param name="Downvotes">The metadata containing the amount of downvotes the suggestion has.</param>
        /// <returns>The current status of the suggestion - whether it is now to be passed, denied or whether it remains.</returns>
        
        public SuggestionVotes CheckVotes(ReactionMetadata Upvotes, ReactionMetadata Downvotes) {
            if (Upvotes.ReactionCount - Downvotes.ReactionCount >= ProposalConfiguration.ReactionPass)
                return SuggestionVotes.Pass;
            
            if (Downvotes.ReactionCount - Upvotes.ReactionCount >= ProposalConfiguration.ReactionPass)
                return SuggestionVotes.Fail;

            return SuggestionVotes.Remain;
        }

        /// <summary>
        /// The Update Suggestion method updates a suggestion to a status set through the SuggestionStatus enum argument,
        /// and then subsequencely updates the related message and staff suggestion messages.
        /// </summary>
        /// <param name="Proposal">The proposal object which has had the status applied to it.</param>
        /// <param name="ProposalStatus">The status of which you wish to apply to the proposal.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task UpdateProposal(Proposal Proposal, ProposalStatus ProposalStatus) {
            Proposal.ProposalStatus = ProposalStatus;
            await ProposalDB.SaveChangesAsync();

            switch (Proposal.ProposalType) {
                case ProposalType.Suggestion:
                    Suggestion Suggestion = ProposalDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.Tracker == Proposal.Tracker).FirstOrDefault();

                    await UpdateSpecificProposal(Proposal, ProposalConfiguration.SuggestionsChannel, Proposal.MessageID);
                    await UpdateSpecificProposal(Proposal, ProposalConfiguration.StaffSuggestionsChannel, Suggestion.StaffMessageID);
                    break;
                case ProposalType.AdminConfirmation:
                    await UpdateSpecificProposal(Proposal, BotConfiguration.ModerationLogChannelID, Proposal.MessageID);

                    if (ProposalStatus == ProposalStatus.Approved) {
                        AdminConfirmation Confirmation = ProposalDB.AdminConfirmations.AsQueryable().Where(Confirmation => Confirmation.Tracker == Proposal.Tracker).FirstOrDefault();

                        Dictionary<string, string> Parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Confirmation.CallbackParameters);
                        Type Class = Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.Name.Equals(Confirmation.CallbackClass)).FirstOrDefault();

                        if (Class.GetMethod(Confirmation.CallbackMethod) == null)
                            throw new NoNullAllowedException("The callback method specified for the admin confirmation is null! This could very well be due to the method being private.");
                        
                        Class.GetMethod(Confirmation.CallbackMethod).Invoke(ServiceProvider.GetRequiredService(Class), new object[1] { Parameters });
                    }

                    break;
            }
        }

        /// <summary>
        /// The Update Specific Proposal method updates a message with the current built embed of the suggestion object.
        /// </summary>
        /// <param name="Proposal">The proposal you wish to set the embed of the message to.</param>
        /// <param name="Channel">The channel in which the message is located.</param>
        /// <param name="MessageID">The ID of the message you wish to change the embed of to update it properly.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task UpdateSpecificProposal(Proposal Proposal, ulong Channel, ulong MessageID) {
            if (Channel == 0 || MessageID == 0)
                return;

            SocketChannel SocketChannel = DiscordSocketClient.GetChannel(Channel);

            if (SocketChannel is SocketTextChannel TextChannel) {
                IMessage ProposalMessage = await TextChannel.GetMessageAsync(MessageID);

                if (ProposalMessage is IUserMessage ProposalMSG) {
                    await ProposalMessage.RemoveAllReactionsAsync();
                    await ProposalMSG.ModifyAsync(SuggestionMSG => SuggestionMSG.Embed = BuildProposal(Proposal).Build());
                } else
                    throw new Exception($"Woa, this is strange! The message required isn't a socket user message! Are you sure this message exists? TopicType: {ProposalMessage.GetType()}");
            } else
                throw new Exception($"Eek! The given channel of {SocketChannel} turned out *not* to be an instance of SocketTextChannel, rather {SocketChannel.GetType().Name}!");
        }

        /// <summary>
        /// The Create Token method creates a random token, the length of which is supplied in the proposal
        /// configuration class, that is not in the ProposalDB already.
        /// </summary>
        /// <returns>A randomly generated token in the form of a string that is not in the proposal database already.</returns>
        
        public string CreateToken() {
            char[] TokenArray = new char[ProposalConfiguration.TrackerLength];

            for (int i = 0; i < TokenArray.Length; i++)
                TokenArray[i] = ProposalConfiguration.RandomCharacters[Random.Next(ProposalConfiguration.RandomCharacters.Length)];

            string Token = new (TokenArray);

            if (ProposalDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.Tracker == Token).FirstOrDefault() == null) {
                return Token;
            } else
                return CreateToken();
        }

        /// <summary>
        /// The BuildProposal method takes a Proposal in as its input and generates an embed from it.
        /// It sets the color based on the proposal status, the title to the status, and fills the rest in
        /// with the related proposal fields.
        /// </summary>
        /// <param name="Proposal">The proposal of which you wish to generate the embed from.</param>
        /// <returns>An automatically generated embed based on the input proposal's fields.</returns>
        
        public EmbedBuilder BuildProposal(Proposal Proposal) {
            Color Color = Proposal.ProposalStatus switch {
                ProposalStatus.Suggested => Color.Blue,
                ProposalStatus.Pending => Color.Orange,
                ProposalStatus.Approved => Color.Green,
                ProposalStatus.Declined => Color.Red,
                ProposalStatus.Deleted => Color.Magenta,
                _ => Color.Teal
            };

            return new EmbedBuilder()
                .WithTitle(Proposal.ProposalStatus.ToString().ToUpper())
                .WithColor(Color)
                .WithThumbnailUrl(DiscordSocketClient.GetUser(Proposal.Proposer).GetAvatarUrl())
                .WithTitle(Proposal.ProposalStatus.ToString().ToUpper())
                .WithDescription(Proposal.Content)
                .WithImageUrl(Proposal.ProxyURL)
                .AddField(!string.IsNullOrEmpty(Proposal.Reason), "Reason:", Proposal.Reason)
                .WithAuthor(DiscordSocketClient.GetUser(Proposal.Proposer))
                .WithCurrentTimestamp()
                .WithFooter(Proposal.Tracker);
        }

    }

}

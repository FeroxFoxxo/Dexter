using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Suggestions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {
    /// <summary>
    /// The Suggestion service, which is used to create and update suggestions on the change of a reaction.
    /// </summary>
    public class SuggestionService : InitializableModule {

        private readonly DiscordSocketClient Client;
        private readonly SuggestionConfiguration SuggestionConfiguration;
        private readonly SuggestionDB SuggestionDB;
        private readonly string RandomCharacters;
        private readonly Random Random;

        /// <summary>
        /// The constructor for the SuggestionService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// It also creates the list of random characters and a new instance of the Random class, which can be used to randomly generate a token.
        /// </summary>
        /// <param name="Client">An instance of the DiscordSocketClient, which is used to hook into events like the MessageRecieved, ReactionAdd and ReactionRemoved events.</param>
        /// <param name="SuggestionConfiguration">The SuggestionConfiguration, which contains the location of the emoji storage guild, as well as IDs of channels.</param>
        /// <param name="SuggestionDB">An instance of the SuggestionDB, which is used as a storage for the suggestions.</param>
        /// <param name="BotConfiguration">The BotConfiguration, which is given to the base method for use when needed to create a generic embed.</param>
        public SuggestionService(DiscordSocketClient Client, SuggestionConfiguration SuggestionConfiguration,
                SuggestionDB SuggestionDB, BotConfiguration BotConfiguration) : base (BotConfiguration) {

            this.Client = Client;
            this.SuggestionConfiguration = SuggestionConfiguration;
            this.SuggestionDB = SuggestionDB;

            RandomCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random = new Random();
        }

        /// <summary>
        /// The AddDelegates method hooks the client MessageReceived, ReactionAdded and ReactionRemoved events and sets them to their related delegates.
        /// </summary>
        public override void AddDelegates() {
            Client.MessageReceived += MessageRecieved;
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
        }

        /// <summary>
        /// The ReactionAdded method checks to see if reaction is added in the suggestion channel and, if so, it runs CheckAsync.
        /// </summary>
        /// <param name="CachedMessage">The cached message the reaction was applied to, used to get the suggestion it relates to.</param>
        /// <param name="Channel">The channel of which the reaction was added to.</param>
        /// <param name="Reaction">The reaction of question which was added.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (Channel.Id != SuggestionConfiguration.SuggestionsChannel || Reaction.User.Value.IsBot)
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
        /// <param name="Channel">The channel of which the reaction was removed from.</param>
        /// <param name="Reaction">The reaction of question which was removed.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (Channel.Id != SuggestionConfiguration.SuggestionsChannel || Reaction.User.Value.IsBot)
                return;

            IUserMessage Message = await CachedMessage.GetOrDownloadAsync();

            if (Message == null)
                throw new Exception("Message does not exist in cache and could not be downloaded! Aborting...");

            await CheckAsync(Message, Reaction);
        }

        /// <summary>
        /// The MessageRecieved method runs when a message is sent in the suggestions channel, and converts the message to a suggestion object.
        /// This suggestion object is then sent back to the channel once deleted as a formatted embed for use to vote on.
        /// </summary>
        /// <param name="Message">A SocketMessage object, which contains details about the message such as its content and attachments.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task MessageRecieved(SocketMessage Message) {
            // Check to see if the message has been sent in the suggestion channel and is not a bot, least we return.
            if (Message.Channel.Id != SuggestionConfiguration.SuggestionsChannel || Message.Author.IsBot)
                return;

            // Check to see if the embed message length is more than 1750, else will fail the embed from sending due to character limits.
            if (Message.Content.Length > 1750) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Your suggestion is too big!")
                    .WithDescription("Please try to summarise your suggestion a little! " +
                    "Keep in mind that emoji add a lot of characters to your suggestion - even if it doesn't seem like it - " +
                    "as Discord handles emoji differently to text, so if you're using a lot of emoji try to cut down on those! <3")
                    .SendEmbed(Message.Author);

                await Message.DeleteAsync();
                return;
            }

            // Creates a new Suggestion object with the related fields.
            Suggestion Suggested = new Suggestion() {
                Content = Message.Content,
                Status = SuggestionStatus.Suggested,
                Suggestor = Message.Author.Id,
                TrackerID = CreateToken()
            };

            RestUserMessage Embed;

            // Add related attachments to the embed.
            if (Message.Attachments.Count > 0)
                Embed = await Message.Channel.SendFileAsync(
                    Message.Attachments.First().ProxyUrl,
                    embed: BuildSuggestion(Suggested).Build()
                );
            else
                Embed = await Message.Channel.SendMessageAsync(embed: BuildSuggestion(Suggested).Build());

            // Delete the message sent by the user.
            await Message.DeleteAsync();

            // Set the message ID in the suggestion object to the ID of the embed.
            Suggested.MessageID = Embed.Id;

            // Add the suggestion object to the database.
            SuggestionDB.Suggestions.Add(Suggested);

            await SuggestionDB.SaveChangesAsync();

            // Add the related emoji specified in the SuggestionConfiguration to the suggestion.
            SocketGuild Guild = Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild);

            foreach (string Emoji in SuggestionConfiguration.SuggestionEmoji) {
                GuildEmote Emote = await Guild.GetEmoteAsync(SuggestionConfiguration.Emoji[Emoji]);
                await Embed.AddReactionAsync(Emote);
            }
        }

        /// <summary>
        /// The Check Async method checks to see whether or not a suggestion is passing or has been denied in the suggestions channel.
        /// It also handles the removing of the embed through the trash can emoji.
        /// </summary>
        /// <param name="Message">The message that has had the emoji applied or removed from it.</param>
        /// <param name="Reaction">The reaction that was applied.</param>
        /// <returns>A boolean value of whether or not you should remove the emoji from after the method has run.</returns>
        public async Task<bool> CheckAsync(IUserMessage Message, SocketReaction Reaction) {
            // Get the suggestion from the database which has a message ID which matches the one of which we're looking for.
            Suggestion Suggested = SuggestionDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.MessageID == Message.Id).FirstOrDefault();

            // Check if the suggestion is not null.
            if (Suggested == null)
                throw new Exception("Suggestion does not exist in database! Aborting...");

            // Check the current amount of upvotes the message has.
            ReactionMetadata Upvotes = Message.Reactions[
                await Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild)
                .GetEmoteAsync(SuggestionConfiguration.Emoji["Upvote"])
            ];

            // Check the current amount of downvotes the message has.
            ReactionMetadata Downvotes = Message.Reactions[
                await Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild)
                .GetEmoteAsync(SuggestionConfiguration.Emoji["Downvote"])
            ];

            // Check if the suggestion has enough upvotes to trigger the suggestion to pass,
            // or if the suggestion has enough downvotes to force it to deny itself.
            switch (CheckVotes(Upvotes, Downvotes)) {
                case SuggestionVotes.Pass:
                    // Set the suggestion to pending if it has passed.
                    await UpdateSuggestion(Suggested, SuggestionStatus.Pending);

                    // Create a new embed in the staff suggestions channel of the current suggestion.
                    RestUserMessage StaffSuggestion = await Client.GetGuild(SuggestionConfiguration.SuggestionGuild)
                        .GetTextChannel(SuggestionConfiguration.StaffSuggestionsChannel)
                        .SendMessageAsync(embed: BuildSuggestion(Suggested).Build());

                    // Set the staff message ID in the suggestions database to the new suggestion.
                    Suggested.StaffMessageID = StaffSuggestion.Id;
                    await SuggestionDB.SaveChangesAsync();

                    // Get the staff suggestions channel and add the related emoji to the message.
                    SocketGuild EmojiCacheGuild = Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild);

                    foreach (string Emoji in SuggestionConfiguration.StaffSuggestionEmoji) {
                        GuildEmote EmoteStaff = await EmojiCacheGuild.GetEmoteAsync(SuggestionConfiguration.Emoji[Emoji]);
                        await StaffSuggestion.AddReactionAsync(EmoteStaff);
                    }

                    break;
                case SuggestionVotes.Fail:
                    // If the suggestion has been declined by the community, set the reason of the suggestion to be
                    // "Declined by the community." and update the suggestion to a status of having been declined.
                    Suggested.Reason = "Declined by the community.";
                    await UpdateSuggestion(Suggested, SuggestionStatus.Declined);
                    break;
                case SuggestionVotes.Remain:
                    break;
            }

            // Run through the emote that has been applied to the suggestions channel to see if the ID of the emote matches one of our designated suggestion emoji. 
            if (Reaction.Emote is Emote Emote)
                foreach (KeyValuePair<string, ulong> Emotes in SuggestionConfiguration.Emoji)
                    if (Emotes.Value == Emote.Id) {
                        switch (Emotes.Key) {
                            case "Upvote":
                            case "Downvote":
                                // If an upvote or downvote has been applied by the suggestor, remove it.
                                if (Suggested.Suggestor != Reaction.UserId)
                                    return false;
                                break;
                            case "Bin":
                                // If the suggestion has had the bin icon applied by the user, set the status of the suggestion to "deleted" and delete the message.
                                if (Suggested.Suggestor == Reaction.UserId) {
                                    await UpdateSuggestion(Suggested, SuggestionStatus.Deleted);
                                    await Message.DeleteAsync();
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
            if (Upvotes.ReactionCount - Downvotes.ReactionCount >= SuggestionConfiguration.ReactionPass)
                return SuggestionVotes.Pass;
            
            if (Downvotes.ReactionCount - Upvotes.ReactionCount >= SuggestionConfiguration.ReactionPass)
                return SuggestionVotes.Fail;

            return SuggestionVotes.Remain;
        }

        /// <summary>
        /// The Update Suggestion method updates a suggestion to a status set through the SuggestionStatus enum argument,
        /// and then subsequencely updates the related message and staff suggestion messages.
        /// </summary>
        /// <param name="Suggestion">The suggestion object which has had the status applied to it.</param>
        /// <param name="Status">The status of which you wish to apply to the suggestion.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task UpdateSuggestion(Suggestion Suggestion, SuggestionStatus Status) {
            Suggestion.Status = Status;
            await SuggestionDB.SaveChangesAsync();

            await UpdateSpecificSuggestion(Suggestion, SuggestionConfiguration.SuggestionsChannel, Suggestion.MessageID);
            await UpdateSpecificSuggestion(Suggestion, SuggestionConfiguration.StaffSuggestionsChannel, Suggestion.StaffMessageID);
        }

        /// <summary>
        /// The Update Specific Suggestion method updates a message with the current built embed of the suggestion object.
        /// </summary>
        /// <param name="Suggestion">The suggestion you wish to set the embed of the message to.</param>
        /// <param name="Channel">The channel in which the message is located.</param>
        /// <param name="MessageID">The ID of the message you wish to change the embed of to update it properly.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task UpdateSpecificSuggestion(Suggestion Suggestion, ulong Channel, ulong MessageID) {
            if (Channel == 0 || MessageID == 0)
                return;

            IMessage SuggestionMessage = await Client.GetGuild(SuggestionConfiguration.SuggestionGuild).GetTextChannel(Channel).GetMessageAsync(MessageID);

            if (SuggestionMessage is RestUserMessage SuggestionMSG) {
                await SuggestionMessage.RemoveAllReactionsAsync();
                await SuggestionMSG.ModifyAsync(SuggestionMSG => SuggestionMSG.Embed = BuildSuggestion(Suggestion).Build());
            } else if (SuggestionMessage is SocketUserMessage SuggestionMSG2) {
                await SuggestionMessage.RemoveAllReactionsAsync();
                await SuggestionMSG2.ModifyAsync(SuggestionMSG => SuggestionMSG.Embed = BuildSuggestion(Suggestion).Build());
            } else
                throw new Exception($"Woa, this is strange! The message required isn't a socket user message! Are you sure this message exists? Type: {SuggestionMessage.GetType()}");
        }

        /// <summary>
        /// The Create Token method creates a random token, the length of which is supplied in the suggestion
        /// configuration class, that is not in the SuggestionDB already.
        /// </summary>
        /// <returns>A randomly generated token in the form of a string that is not in the suggestion database already.</returns>
        public string CreateToken() {
            char[] TokenArray = new char[SuggestionConfiguration.TrackerLength];

            for (int i = 0; i < TokenArray.Length; i++)
                TokenArray[i] = RandomCharacters[Random.Next(RandomCharacters.Length)];

            string Token = new string(TokenArray);

            if (SuggestionDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.TrackerID == Token).FirstOrDefault() == null)
                return Token;
            else
                return CreateToken();
        }

        /// <summary>
        /// The BuildSuggestion method takes a Suggestion in as its input and generates an embed from it.
        /// It sets the color based on the suggestion status, the title to the status, and fills the rest in
        /// with the related suggestion fields.
        /// </summary>
        /// <param name="Suggestion">The suggestion of which you wish to generate the embed from.</param>
        /// <returns>An automatically generated embed based on the input suggestion's fields.</returns>
        public EmbedBuilder BuildSuggestion(Suggestion Suggestion) {
            Color Color = Suggestion.Status switch {
                SuggestionStatus.Suggested => Color.Blue,
                SuggestionStatus.Pending => Color.Orange,
                SuggestionStatus.Approved => Color.Green,
                SuggestionStatus.Declined => Color.Red,
                SuggestionStatus.Deleted => Color.Magenta,
                _ => Color.Teal
            };

            return new EmbedBuilder()
                .WithTitle(Suggestion.Status.ToString().ToUpper())
                .WithColor(Color)
                .WithThumbnailUrl(Client.GetUser(Suggestion.Suggestor).GetAvatarUrl())
                .WithTitle(Suggestion.Status.ToString().ToUpper())
                .WithDescription(Suggestion.Content)
                .AddField(!string.IsNullOrEmpty(Suggestion.Reason), "Reason:", Suggestion.Reason)
                .WithAuthor(Client.GetUser(Suggestion.Suggestor))
                .WithCurrentTimestamp()
                .WithFooter(Suggestion.TrackerID);
        }

    }
}

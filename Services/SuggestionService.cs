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
    public class SuggestionService : InitializableModule {

        private readonly DiscordSocketClient Client;
        private readonly SuggestionConfiguration SuggestionConfiguration;
        private readonly SuggestionDB SuggestionDB;
        private readonly CommandModule Module;
        private readonly string RandomCharacters;
        private readonly Random Random;

        public SuggestionService(DiscordSocketClient _Client, SuggestionConfiguration _SuggestionConfiguration, SuggestionDB _SuggestionDB, CommandModule _Module) {
            Client = _Client;
            SuggestionConfiguration = _SuggestionConfiguration;
            SuggestionDB = _SuggestionDB;
            Module = _Module;
            RandomCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random = new Random();
        }

        public override void AddDelegates() {
            Client.MessageReceived += MessageRecieved;
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> MessageCache, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (IsNotSuggestion(Channel, Reaction))
                return;

            IUserMessage Message = await MessageCache.GetOrDownloadAsync();

            if (Message == null)
                throw new Exception("Message does not exist in cache and could not be downloaded! Aborting...");

            if (await CheckAsync(Message, Reaction))
                await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> MessageCache, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (IsNotSuggestion(Channel, Reaction))
                return;

            IUserMessage Message = await MessageCache.GetOrDownloadAsync();

            if (Message == null)
                throw new Exception("Message does not exist in cache and could not be downloaded! Aborting...");

            await CheckAsync(Message, Reaction);
        }

        private async Task MessageRecieved(SocketMessage Message) {
            if (Message.Channel.Id != SuggestionConfiguration.SuggestionsChannel || Message.Author.IsBot)
                return;

            if (Message.Content.Length > 1750) {
                await Module.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Your suggestion is too big!")
                    .WithDescription("Please try to summarise your suggestion a little! " +
                    "Keep in mind that emoji add a lot of characters to your suggestion - even if it doesn't seem like it - " +
                    "as Discord handles emoji differently to text, so if you're using a lot of emoji try to cut down on those! <3")
                    .SendEmbed(Message.Author);

                await Message.DeleteAsync();
                return;
            }

            Suggestion Suggested = new Suggestion() {
                Content = Message.Content,
                Status = SuggestionStatus.Suggested,
                Suggestor = Message.Author.Id,
                TrackerID = CreateToken()
            };

            RestUserMessage Embed;

            if (Message.Attachments.Count > 0)
                Embed = await Message.Channel.SendFileAsync(
                    Message.Attachments.First().ProxyUrl,
                    embed: BuildSuggestion(Suggested).Build()
                );
            else
                Embed = await Message.Channel.SendMessageAsync(embed: BuildSuggestion(Suggested).Build());

            await Message.DeleteAsync();

            Suggested.MessageID = Embed.Id;

            SuggestionDB.Suggestions.Add(Suggested);

            await SuggestionDB.SaveChangesAsync();

            SocketGuild Guild = Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild);

            foreach (string Emoji in SuggestionConfiguration.SuggestionEmoji) {
                GuildEmote Emote = await Guild.GetEmoteAsync(SuggestionConfiguration.Emoji[Emoji]);
                await Embed.AddReactionAsync(Emote);
            }
        }

        private bool IsNotSuggestion(ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (Channel.Id != SuggestionConfiguration.SuggestionsChannel || Reaction.User.Value.IsBot)
                return true;
            return false;
        }

        private async Task<bool> CheckAsync(IUserMessage Message, SocketReaction Reaction) {
            Suggestion Suggested = SuggestionDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.MessageID == Message.Id).FirstOrDefault();

            if (Suggested == null)
                throw new Exception("Suggestion does not exist in database! Aborting...");

            if (Suggested.Status != SuggestionStatus.Suggested)
                return true;

            ReactionMetadata Upvotes = Message.Reactions[
                await Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild)
                .GetEmoteAsync(SuggestionConfiguration.Emoji["Upvote"])
            ];

            ReactionMetadata Downvotes = Message.Reactions[
                await Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild)
                .GetEmoteAsync(SuggestionConfiguration.Emoji["Downvote"])
            ];

            if (Reaction.Emote is Emote Emote)
                foreach (KeyValuePair<string, ulong> Emotes in SuggestionConfiguration.Emoji)
                    if (Emotes.Value == Emote.Id) {
                        switch (Emotes.Key) {
                            case "Upvote":
                            case "Downvote":
                                if (Suggested.Suggestor == Reaction.UserId) {
                                    switch (CheckVotes(Upvotes, Downvotes)) {
                                        case SuggestionVotes.Pass:
                                            await UpdateSuggestion(Suggested, SuggestionStatus.Pending);
                                            RestUserMessage StaffSuggestion = await Client.GetGuild(SuggestionConfiguration.SuggestionGuild)
                                                .GetTextChannel(SuggestionConfiguration.StaffSuggestionsChannel)
                                                .SendMessageAsync(embed: BuildSuggestion(Suggested).Build());

                                            Suggested.StaffMessageID = StaffSuggestion.Id;
                                            await SuggestionDB.SaveChangesAsync();

                                            SocketGuild Guild = Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild);

                                            foreach (string Emoji in SuggestionConfiguration.StaffSuggestionEmoji) {
                                                GuildEmote EmoteStaff = await Guild.GetEmoteAsync(SuggestionConfiguration.Emoji[Emoji]);
                                                await StaffSuggestion.AddReactionAsync(EmoteStaff);
                                            }

                                            break;
                                        case SuggestionVotes.Fail:
                                            Suggested.Reason = "Declined by the community.";
                                            await UpdateSuggestion(Suggested, SuggestionStatus.Declined);
                                            break;
                                        case SuggestionVotes.Remain:
                                            break;
                                    }
                                    return false;
                                }
                                break;
                            case "Bin":
                                if (Suggested.Suggestor == Reaction.UserId) {
                                    await UpdateSuggestion(Suggested, SuggestionStatus.Deleted);
                                    await Message.DeleteAsync();
                                    return false;
                                }
                                break;
                            default:
                                throw new Exception("Unknown reaction exists in JSON config but is not assigned the correct name! " +
                                    $"Please make sure that the reaction {Emotes.Key} is assigned the correct value in {GetType().Name}.");
                        }
                    }
            return true;
        }

        private SuggestionVotes CheckVotes(ReactionMetadata Upvotes, ReactionMetadata Downvotes) {
            if (Upvotes.ReactionCount - Downvotes.ReactionCount >= SuggestionConfiguration.ReactionPass)
                return SuggestionVotes.Pass;
            
            if (Downvotes.ReactionCount - Upvotes.ReactionCount >= SuggestionConfiguration.ReactionPass)
                return SuggestionVotes.Fail;

            return SuggestionVotes.Remain;
        }

        public async Task UpdateSuggestion(Suggestion Suggestion, SuggestionStatus Status) {
            Suggestion.Status = Status;
            await SuggestionDB.SaveChangesAsync();

            await UpdateSpecificSuggestion(Suggestion, SuggestionConfiguration.SuggestionsChannel, Suggestion.MessageID);
            await UpdateSpecificSuggestion(Suggestion, SuggestionConfiguration.StaffSuggestionsChannel, Suggestion.StaffMessageID);
        }

        private async Task UpdateSpecificSuggestion(Suggestion Suggestion, ulong Channel, ulong MessageID) {
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
        private string CreateToken() {
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

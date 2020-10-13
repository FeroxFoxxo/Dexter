using Dexter.Abstractions;
using Dexter.Configurations;
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
                    embed: BuildSuggestion(Suggested)
                );
            else
                Embed = await Message.Channel.SendMessageAsync(embed: BuildSuggestion(Suggested));

            await Message.DeleteAsync();

            Suggested.MessageID = Embed.Id;

            SuggestionDB.Suggestions.Add(Suggested);
            await SuggestionDB.SaveChangesAsync();

            SocketGuild Guild = Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild);

            foreach (ulong EmoteReactions in SuggestionConfiguration.Emoji.Values)
                await Embed.AddReactionAsync(await Guild.GetEmoteAsync(EmoteReactions));
        }

        private bool IsNotSuggestion(ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (Channel.Id != SuggestionConfiguration.SuggestionsChannel || Reaction.User.Value.IsBot)
                return true;
            return false;
        }

        private async Task<bool> CheckAsync(IUserMessage Message, SocketReaction Reaction) {
            Suggestion Suggested = SuggestionDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.MessageID == Message.Id).FirstOrDefault();

            if (Message == null)
                throw new Exception("Suggestion does not exist in database! Aborting...");

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
                                if (Suggested.Suggestor != Reaction.UserId) {
                                    switch (CheckVotes(Upvotes, Downvotes)) {
                                        case SuggestionVotes.Pass:
                                            await UpdateSuggestion(Suggested, SuggestionStatus.Pending);
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

        private async Task UpdateSuggestion(Suggestion Suggestion, SuggestionStatus Status) {
            Suggestion.Status = Status;
            await SuggestionDB.SaveChangesAsync();

            IMessage SuggestionMessage = await Client.GetGuild(SuggestionConfiguration.SuggestionGuild)
                .GetTextChannel(SuggestionConfiguration.SuggestionsChannel).GetMessageAsync(Suggestion.MessageID);

            if (SuggestionMessage is RestUserMessage SuggestionMSG) {
                await SuggestionMessage.RemoveAllReactionsAsync();
                await SuggestionMSG.ModifyAsync(SuggestionMSG => SuggestionMSG.Embed = BuildSuggestion(Suggestion));
            } else if (SuggestionMessage is SocketUserMessage SuggestionMSG2) {
                await SuggestionMessage.RemoveAllReactionsAsync();
                await SuggestionMSG2.ModifyAsync(SuggestionMSG => SuggestionMSG.Embed = BuildSuggestion(Suggestion));
            } else
                throw new Exception($"Woa, this is strange! The message required isn't a socket user message! Are you sure this message exists? Type: {SuggestionMessage.GetType()}");
        }

        private string CreateToken() {
            char[] TokenArray = new char[8];

            for (int i = 0; i < TokenArray.Length; i++)
                TokenArray[i] = RandomCharacters[Random.Next(RandomCharacters.Length)];

            string Token = new string(TokenArray);

            if (SuggestionDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.TrackerID == Token).FirstOrDefault() == null)
                return Token;
            else
                return CreateToken();
        }

        private Embed BuildSuggestion(Suggestion Suggestion) {
            EmbedBuilder Embed = new EmbedBuilder()
                .WithTitle(Suggestion.Status.ToString().ToUpper())
                .WithColor(new Color(Convert.ToUInt32(SuggestionConfiguration.SuggestionColors[Suggestion.Status.ToString()], 16)))
                .WithThumbnailUrl(Client.GetUser(Suggestion.Suggestor).GetAvatarUrl())
                .WithTitle(Suggestion.Status.ToString().ToUpper())
                .WithDescription(Suggestion.Content)
                .WithAuthor(Client.GetUser(Suggestion.Suggestor))
                .WithCurrentTimestamp()
                .WithFooter(Suggestion.TrackerID);

            if (!string.IsNullOrEmpty(Suggestion.Reason))
                Embed.AddField("Reason", Suggestion.Reason);

            return Embed.Build();
        }

    }
}

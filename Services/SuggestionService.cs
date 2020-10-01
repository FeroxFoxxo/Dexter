using Dexter.Configurations;
using Dexter.Core.Abstractions;
using Dexter.Databases;
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
            if (Channel.Id != SuggestionConfiguration.SuggestionsChannel)
                return;

            IUserMessage Message = await MessageCache.GetOrDownloadAsync();

            if (Message == null)
                throw new Exception("Suggestion message does not exist in cache and could not be downloaded! Aborting...");

            if (Reaction.Emote is Emote Emote)
                foreach(KeyValuePair<string, ulong> Emotes in SuggestionConfiguration.Emoji)
                    if(Emotes.Value == Emote.Id) {
                        Suggestion Suggested = SuggestionDB.Suggestions.AsQueryable().Where(Suggestion => Suggestion.MessageID == Message.Id).FirstOrDefault();

                        if (Suggested == null)
                            throw new Exception("Haiya, it doesn't seem like this message exists in the database!");

                        switch (Emotes.Key) {
                            case "Upvote":
                                if(Suggested.Suggestor != Reaction.UserId) {

                                    return;
                                }
                                break;
                            case "Downvote":
                                if (Suggested.Suggestor != Reaction.UserId) {

                                    return;
                                }
                                break;
                            case "Bin":
                                if (Suggested.Suggestor == Reaction.UserId) {

                                    return;
                                }
                                break;
                            default:
                                throw new Exception("Unknown reaction exists in JSON config but is not assigned the correct name! " +
                                    $"Please make sure that the reaction {Emotes.Key} is assigned the correct value in {GetType().Name}.");
                        }
                    }

            if(Reaction.User.GetValueOrDefault() != null)
                await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.GetValueOrDefault());
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (Channel.Id != SuggestionConfiguration.SuggestionsChannel)
                return;

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

            RestUserMessage Embed = await Message.Channel.SendMessageAsync(
                embed: BuildSuggestion(Suggested)
            );

            await Message.DeleteAsync();

            Suggested.MessageID = Embed.Id;

            SuggestionDB.Suggestions.Add(Suggested);
            await SuggestionDB.SaveChangesAsync();

            SocketGuild Guild = Client.GetGuild(SuggestionConfiguration.EmojiStorageGuild);

            foreach (ulong EmoteReactions in SuggestionConfiguration.Emoji.Values)
                await Embed.AddReactionAsync(await Guild.GetEmoteAsync(EmoteReactions));
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
            return new EmbedBuilder()
                .WithTitle(Suggestion.Status.ToString().ToUpper())
                .WithColor(new Color(Convert.ToUInt32(SuggestionConfiguration.SuggestionColors[Suggestion.Status.ToString()], 16)))
                .WithThumbnailUrl(Client.GetUser(Suggestion.Suggestor).GetAvatarUrl())
                .WithTitle(Suggestion.Status.ToString().ToUpper())
                .WithDescription(Suggestion.Content)
                .WithAuthor(Client.GetUser(Suggestion.Suggestor))
                .WithCurrentTimestamp()
                .WithFooter(Suggestion.TrackerID)
                .Build();
        }
    }
}

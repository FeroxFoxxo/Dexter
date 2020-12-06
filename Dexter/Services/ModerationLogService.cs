﻿using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Extensions;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The ModerationLog Service deals with logging certain events to a channel.
    /// Currently, this only includes the logging of reactions to the channel.
    /// </summary>
    
    public class ModerationLogService : Service {

        /// <summary>
        /// The ModerationLogService is used to find and create the moderation logs webhook.
        /// </summary>
        
        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// The DiscordWebhookClient is used for sending messages to the logging channel.
        /// </summary>

        public DiscordWebhookClient DiscordWebhookClient;

        /// <summary>
        /// The Initialize method adds the ReactionRemoved hook to the ReactionRemovedLog method.
        /// It also hooks the ready event to the CreateWebhook delegate.
        /// </summary>
        
        public override void Initialize() {
            DiscordSocketClient.ReactionRemoved += ReactionRemovedLog;
            DiscordSocketClient.Ready += CreateWebhook;
        }

        /// <summary>
        /// The Create Webhook method runs on Ready and is what initializes our webhook.
        /// </summary>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task CreateWebhook() {
            DiscordWebhookClient = await CreateOrGetWebhook(ModerationConfiguration.WebhookChannel, ModerationConfiguration.WebhookName);
        }

        /// <summary>
        /// The ReactionRemovedLog records if a reaction has been quickly removed from a message, as is important to find if someone has been spamming reactions.
        /// </summary>
        /// <param name="UserMessage">An instance of the message the reaction has been removed from.</param>
        /// <param name="MessageChannel">The channel of which the reaction has been removed in - used to check if it's from a channel that is often removed from.</param>
        /// <param name="Reaction">An object containing the reaction that had been removed.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task ReactionRemovedLog(Cacheable<IUserMessage, ulong> UserMessage, ISocketMessageChannel MessageChannel, SocketReaction Reaction) {
            if (ModerationConfiguration.DisabledReactionChannels.Contains(MessageChannel.Id))
                return;

            IMessage CachedMessage = await UserMessage.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (string.IsNullOrEmpty(CachedMessage.Content))
                return;

            if (DiscordWebhookClient != null)
                await new EmbedBuilder()
                    .WithAuthor(Reaction.User.Value)
                    .WithDescription($"**Reaction removed in <#{MessageChannel.Id}> by {Reaction.User.GetValueOrDefault().GetUserInformation()}**")
                    .AddField("Message", CachedMessage.Content.Length > 50 ? CachedMessage.Content.Substring(0, 50) + "..." : CachedMessage.Content)
                    .AddField("Reaction Removed", Reaction.Emote)
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(DiscordWebhookClient);
        }

    }

}

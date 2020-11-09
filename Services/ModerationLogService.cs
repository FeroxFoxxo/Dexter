using Dexter.Configurations;
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
    public class ModerationLogService : InitializableModule {

        private readonly ModerationConfiguration ModerationConfiguration;
        private readonly DiscordSocketClient Client;
        private readonly DiscordWebhookClient Webhook;

        /// <summary>
        /// The constructor for the ModerationLogService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// The constructor also creates the moderation logging webhook if it doesn't exist already in the moderation logs channel.
        /// </summary>
        /// <param name="Client">The current instance of the DiscordSocketClient, which is used to hook into the ReactionRemoved delegate.</param>
        /// <param name="ModerationConfiguration">The instance of the ModerationLogService, which is used to find and create the moderation logs webhook.</param>
        /// <param name="BotConfiguration">The BotConfiguration, which is given to the base method for use when needed to create a generic embed.</param>
        public ModerationLogService(DiscordSocketClient Client, ModerationConfiguration ModerationConfiguration, BotConfiguration BotConfiguration) : base (BotConfiguration) {
            this.ModerationConfiguration = ModerationConfiguration;
            this.Client = Client;

            if (!string.IsNullOrEmpty(ModerationConfiguration.ModerationWebhookURL))
                Webhook = new DiscordWebhookClient(ModerationConfiguration.ModerationWebhookURL);
        }

        /// <summary>
        /// The AddDelegates method adds the ReactionRemoved hook to the ReactionRemovedLog method.
        /// </summary>
        public override void AddDelegates() {
            Client.ReactionRemoved += ReactionRemovedLog;
        }

        /// <summary>
        /// The ReactionRemovedLog records if a reaction has been quickly removed from a message, as is important to find if someone has been spamming reactions.
        /// </summary>
        /// <param name="Message">An instance of the message the reaction has been removed from.</param>
        /// <param name="Channel">The channel of which the reaction has been removed in - used to check if it's from a channel that is often removed from.</param>
        /// <param name="Reaction">An object containing the reaction that had been removed.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task ReactionRemovedLog(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (ModerationConfiguration.DisabledReactionChannels.Contains(Channel.Id))
                return;

            IMessage CachedMessage = await Message.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (Webhook != null)
                await new EmbedBuilder()
                    .WithAuthor(Reaction.User.Value)
                    .WithDescription($"**Reaction removed in <#{Channel.Id}> by {Reaction.User.GetValueOrDefault().GetUserInformation()}**")
                    .AddField("Message", CachedMessage.Content.Length > 50 ? CachedMessage.Content.Substring(0, 50) + "..." : CachedMessage.Content)
                    .AddField("Reaction Removed", Reaction.Emote)
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(Webhook);
        }

    }
}

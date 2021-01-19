using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Extensions;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The MeetNGreet service is used to log messages that have been updated and deleted from the MeetNGreet channel.
    /// It does this through creating a webhook and sending to that very webhook each time an event runs.
    /// </summary>
    
    public class MeetNGreetService : Service {

        /// <summary>
        /// The MNGConfiguration is used to find and create the MNG webhook.
        /// </summary>
        
        public MNGConfiguration MNGConfiguration { get; set; }

        /// <summary>
        /// The DiscordWebhookClient is used for sending messages to the mng logging channel.
        /// </summary>

        public DiscordWebhookClient DiscordWebhookClient;

        /// <summary>
        /// The Initialize method adds the MessageDeleted and MessageUpdated hooks into their respective MNG check methods.
        /// It also hooks the ready event to the CreateWebhook delegate.
        /// </summary>
        
        public override void Initialize() {
            DiscordSocketClient.MessageDeleted += MNGMessageDeleted;
            DiscordSocketClient.MessageUpdated += MNGMessageUpdated;
            DiscordSocketClient.Ready += CreateWebhook;
        }

        /// <summary>
        /// The Create Webhook method runs on Ready and is what initializes our webhook.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task CreateWebhook() {
            DiscordWebhookClient = await CreateOrGetWebhook(MNGConfiguration.WebhookChannel, MNGConfiguration.WebhookName);
        }

        /// <summary>
        /// The MNGMessageUpdated method check if a message is edited in the MNG channel and, if so,
        /// it uses the MNG webhook to send details pertaining to the previous message, the now updated message,
        /// the author of the message, the ID of the message, and a link to quickly scroll to the message in question.
        /// </summary>
        /// <param name="OldMessage">An object of the previous message that had been edited.</param>
        /// <param name="NewMessage">The instance of the new, changed message.</param>
        /// <param name="SocketMessageChannel">The channel from which the message had been sent from.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task MNGMessageUpdated(Cacheable<IMessage, ulong> OldMessage, SocketMessage NewMessage, ISocketMessageChannel SocketMessageChannel) {
            if (SocketMessageChannel.Id != MNGConfiguration.MeetNGreetChannel)
                return;

            IMessage CachedMessage = await OldMessage.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (CachedMessage.Author.IsBot)
                return;

            if (DiscordWebhookClient != null)
                await new EmbedBuilder()
                    .WithAuthor(CachedMessage.Author)
                    .WithDescription($"**Message edited in <#{SocketMessageChannel.Id}>** [Jump to message](https://discordapp.com/channels/{ (NewMessage.Channel as SocketGuildChannel).Guild.Id }/{ NewMessage.Channel.Id }/{ NewMessage.Id })")
                    .AddField("Before", CachedMessage.Content.Length > 1000 ? CachedMessage.Content.Substring(0, 1000) + "..." : CachedMessage.Content)
                    .AddField("After", NewMessage.Content.Length > 1000 ? NewMessage.Content.Substring(0, 1000) + "..." : NewMessage.Content)
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(DiscordWebhookClient);
        }

        /// <summary>
        /// The MNGMessageDeleted method checks if a message is deleted in the MNG channel and, if so,
        /// send a message through the webhook containing the message sent, author, ID and content.
        /// </summary>
        /// <param name="DeletedMessage">The message that has been cached from the sent channel.</param>
        /// <param name="Channel">The channel from which the message had been sent from.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task MNGMessageDeleted(Cacheable<IMessage, ulong> DeletedMessage, IChannel Channel) {
            if (Channel.Id != MNGConfiguration.MeetNGreetChannel)
                return;

            IMessage CachedMessage = await DeletedMessage.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (CachedMessage.Author.IsBot)
                return;

            if (DiscordWebhookClient != null)
                await new EmbedBuilder()
                    .WithAuthor(CachedMessage.Author)
                    .WithDescription($"**Message sent by <@{CachedMessage.Author.Id}> deleted in in <#{Channel.Id}>**\n{(CachedMessage.Content.Length > 1900 ? CachedMessage.Content.Substring(0, 1900) + "...": CachedMessage.Content)}")
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(DiscordWebhookClient);
        }

    }

}

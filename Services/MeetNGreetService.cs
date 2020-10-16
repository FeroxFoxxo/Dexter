using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Core.Extensions;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Dexter.Services {
    public class MeetNGreetService : InitializableModule {

        private readonly DiscordSocketClient Client;
        private readonly MNGConfiguration MNGConfig;
        private readonly DiscordWebhookClient Webhook;

        public MeetNGreetService(DiscordSocketClient _Client, MNGConfiguration _MNGConfig) {
            Client = _Client;
            MNGConfig = _MNGConfig;
            
            if(!string.IsNullOrEmpty(_MNGConfig.MeetNGreetWebhookURL))
                Webhook = new DiscordWebhookClient(_MNGConfig.MeetNGreetWebhookURL);
        }

        public override void AddDelegates() {
            Client.MessageDeleted += MNGMessageDeleted;
            Client.MessageUpdated += MNGMessageUpdated;
        }

        private async Task MNGMessageUpdated(Cacheable<IMessage, ulong> OldMessage, SocketMessage NewMessage, ISocketMessageChannel Channel) {
            if (Channel.Id != MNGConfig.MeetNGreetChannel)
                return;

            IMessage CachedMessage = await OldMessage.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (CachedMessage.Author.IsBot)
                return;

            if (Webhook != null)
                await new EmbedBuilder()
                    .WithAuthor(CachedMessage.Author)
                    .WithDescription($"**Message edited in <#{Channel.Id}>** [Jump to message](https://discordapp.com/channels/{ (NewMessage.Channel as SocketGuildChannel).Guild.Id }/{ NewMessage.Channel.Id }/{ NewMessage.Id })")
                    .AddField("Before", CachedMessage.Content.Length > 1000 ? CachedMessage.Content.Substring(0, 1000) + "..." : CachedMessage.Content)
                    .AddField("After", NewMessage.Content.Length > 1000 ? NewMessage.Content.Substring(0, 1000) + "..." : NewMessage.Content)
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(Webhook);
        }

        private async Task MNGMessageDeleted(Cacheable<IMessage, ulong> Message, IChannel Channel) {
            if (Channel.Id != MNGConfig.MeetNGreetChannel)
                return;

            IMessage CachedMessage = await Message.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (CachedMessage.Author.IsBot)
                return;

            if (Webhook != null)
                await new EmbedBuilder()
                    .WithAuthor(CachedMessage.Author)
                    .WithDescription($"**Message sent by <@{CachedMessage.Author.Id}> deleted in in <#{Channel.Id}>**\n{(CachedMessage.Content.Length > 1900 ? CachedMessage.Content.Substring(0, 1900) + "...": CachedMessage.Content)}")
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(Webhook);
        }

    }
}

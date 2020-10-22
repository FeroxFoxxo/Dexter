using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Extensions;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {
    public class ModerationLogService : InitializableModule {

        private readonly ModerationConfiguration ModerationConfiguration;
        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly DiscordWebhookClient Webhook;

        public ModerationLogService(DiscordSocketClient _DiscordSocketClient, ModerationConfiguration _ModerationConfiguration) {
            ModerationConfiguration = _ModerationConfiguration;
            DiscordSocketClient = _DiscordSocketClient;

            if (!string.IsNullOrEmpty(ModerationConfiguration.ModerationWebhookURL))
                Webhook = new DiscordWebhookClient(ModerationConfiguration.ModerationWebhookURL);
        }

        public override void AddDelegates() {
            DiscordSocketClient.ReactionRemoved += ReactionRemovedLog;
        }

        private async Task ReactionRemovedLog(Cacheable<IUserMessage, ulong> OldMessage, ISocketMessageChannel Channel, SocketReaction Reaction) {
            if (ModerationConfiguration.DisabledReactionChannels.Contains(Channel.Id))
                return;

            IMessage CachedMessage = await OldMessage.GetOrDownloadAsync();

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

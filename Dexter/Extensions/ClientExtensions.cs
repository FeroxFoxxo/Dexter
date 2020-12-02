using Dexter.Services;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Dexter.Extensions {

    /// <summary>
    /// The DiscordSocketClient Extensions class offers a variety of different extensions that can be applied to a DiscordSocketClient.
    /// </summary>
    public static class ClientExtensions {

        /// <summary>
        /// The Create Or Get Webhook extension extends upon a client, finds the given channel and, when provided a name, attempts to find a webhook
        /// with that said name. If the webhook can not be found, it creates a new webhook in the channel with the set name.
        /// </summary>
        /// <param name="DiscordSocketClient">The DiscordSocketClient is the DiscordSocketClient used to find the channel that the method extends upon.</param>
        /// <param name="ChannelID">The Channel ID is the snowflake ID of the channel which you wish the webhook to be made in.</param>
        /// <param name="WebhookName">The Webhook Name is the identifier of the webhook, and is what the webhook will be called.</param>
        /// <returns>The DiscordWebhookClient of the webhook that has been gotten or created.</returns>
        public static async Task<DiscordWebhookClient> CreateOrGetWebhook (this DiscordSocketClient DiscordSocketClient, ulong ChannelID, string WebhookName) {
            if (ChannelID <= 0)
                return null;

            SocketChannel Channel = DiscordSocketClient.GetChannel(ChannelID);

            if (Channel is SocketTextChannel TextChannel) {
                foreach (RestWebhook RestWebhook in await TextChannel.GetWebhooksAsync())
                    if (RestWebhook.Name.Equals(WebhookName))
                        return new DiscordWebhookClient(RestWebhook.Id, RestWebhook.Token);

                RestWebhook Webhook = await TextChannel.CreateWebhookAsync(WebhookName, InitializeDependencies.ServiceProvider.GetRequiredService<ProfileService>().GetRandomPFP());

                return new DiscordWebhookClient(Webhook.Id, Webhook.Token);
            }

            throw new Exception($"The webhook {WebhookName} could not be initialized in the given channel {Channel} due to it being of type {Channel.GetType().Name}.");
        }

    }

}

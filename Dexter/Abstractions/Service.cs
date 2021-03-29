using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Services;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Abstractions {

    /// <summary>
    /// The Service is an abstract class that all services extend upon.
    /// Services run when a specified event occured, as is hooked into the client through the Initialize method.
    /// </summary>
    
    public abstract class Service {

        /// <summary>
        /// The ProfileService is used to find a random profile picture for a webhook on create or get.
        /// </summary>
        public ProfilingService ProfileService { get; set; }

        /// <summary>
        /// The BotConfiguration is used to find the thumbnails for the BuildEmbed method.
        /// </summary>
        public BotConfiguration BotConfiguration { get; set; }

        /// <summary>
        /// The DiscordSocketClient is used to create the webhook for the webhook on create or get.
        /// </summary>
        public DiscordSocketClient DiscordSocketClient { get; set; }

        /// <summary>
        /// The TimerService class is used to create a timer for wait until an expiration time has been reached.
        /// </summary>
        public TimerService TimerService { get; set; }

        /// <summary>
        /// The Initialize abstract method is what is called when all dependencies are initialized.
        /// It can be used to hook into delegates to run when an event occurs.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// The Create Or Get Webhook finds the given channel and, when provided a name, attempts to find a webhook
        /// with that said name. If the webhook can not be found, it creates a new webhook in the channel with the set name.
        /// </summary>
        /// <param name="ChannelID">The Channel ID is the snowflake ID of the channel which you wish the webhook to be made in.</param>
        /// <param name="WebhookName">The Webhook Name is the identifier of the webhook, and is what the webhook will be called.</param>
        /// <returns>The DiscordWebhookClient of the webhook that has been gotten or created.</returns>

        public async Task<DiscordWebhookClient> CreateOrGetWebhook(ulong ChannelID, string WebhookName) {
            if (ChannelID > 0)

                try {
                    SocketChannel Channel = DiscordSocketClient.GetChannel(ChannelID);

                    if (Channel is SocketTextChannel TextChannel) {
                        foreach (RestWebhook RestWebhook in await TextChannel.GetWebhooksAsync())
                            if (RestWebhook.Name.Equals(WebhookName))
                                return new DiscordWebhookClient(RestWebhook.Id, RestWebhook.Token);

                        RestWebhook Webhook = await TextChannel.CreateWebhookAsync(WebhookName, ProfileService.GetRandomPFP());

                        return new DiscordWebhookClient(Webhook.Id, Webhook.Token);
                    }

                    throw new Exception($"The webhook {WebhookName} could not be initialized in the given channel {Channel} due to it being of type {Channel.GetType().Name}.");

                } catch (HttpException) { }

            return null;
        }

        /// <summary>
        /// The Create Event Timer method is a generic method that will await for an expiration time to be reached
        /// before continuing execution of the code set in the CallbackMethod parameter.
        /// </summary>
        /// <param name="CallbackMethod">The method you wish to callback once expired.</param>
        /// <param name="CallbackParameters">The parameters you wish to callback with once expired.</param>
        /// <param name="SecondsTillExpiration">The count in seconds until the timer will expire.</param>
        /// <param name="TimerType">The given type of the timer, specifying if it should be removed after the set time (EXPIRE) or continue in the set interval.</param>
        /// <returns>The token assosiated with the timed event for use to refer to.</returns>

        public async Task<string> CreateEventTimer(Func<Dictionary<string, string>, Task> CallbackMethod,
                Dictionary<string, string> CallbackParameters, int SecondsTillExpiration, TimerType TimerType) {

            string JSON = JsonConvert.SerializeObject(CallbackParameters);

            return await TimerService.AddTimer(JSON, CallbackMethod.Target.GetType().Name, CallbackMethod.Method.Name, SecondsTillExpiration, TimerType);
        }

        /// <summary>
        /// The Build Embed method is a generic method that simply calls upon the EMBED BUILDER extension method.
        /// </summary>
        /// <param name="Thumbnail">The thumbnail that you would like to be applied to the embed.</param>
        /// <returns>A new embed builder with the specified attributes applied to the embed.</returns>

        public EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail, BotConfiguration);
        }

    }

}

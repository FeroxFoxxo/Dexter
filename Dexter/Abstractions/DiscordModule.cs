using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Databases.Proposals;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Services;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Abstractions {

    /// <summary>
    /// The DiscordModule class is an abstract class all command modules extend upon.
    /// Command modules contain methods that run on the specified command being entered.
    /// </summary>
    
    public abstract class DiscordModule : ModuleBase<SocketCommandContext> {

        /// <summary>
        /// The ProposalService class is used to send a command to be accepted by an admin through the SendForAdminApproval method.
        /// </summary>
        public ProposalService ProposalService { get; set; }

        /// <summary>
        /// The ReactionMenuService class is used to create a reaction menu for the CreateReactionMenu method.
        /// </summary>
        public ReactionMenuService ReactionMenuService { get; set; }

        /// <summary>
        /// The TimerService class is used to create a timer for wait until an expiration time has been reached.
        /// </summary>
        public TimerService TimerService { get; set; }

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
        /// The Build Embed method is a generic method that simply calls upon the EMBED BUILDER extension method.
        /// </summary>
        /// <param name="Thumbnail">The thumbnail that you would like to be applied to the embed.</param>
        /// <returns>A new embed builder with the specified attributes applied to the embed.</returns>

        public EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail, BotConfiguration);
        }

        /// <summary>
        /// The Send For Admin Approval method is a generic method that will send the related proposal to the administrators
        /// for approval. On approval it will callback the method specified with the given parameters.
        /// </summary>
        /// <param name="CallbackMethod">The method you wish to callback once approved.</param>
        /// <param name="CallbackParameters">The parameters you wish to callback with once approved.</param>
        /// <param name="Author">The author of the message who will be attached to the proposal.</param>
        /// <param name="Proposal">The message that will be attached to the proposal.</param>
        /// <param name="DenyCallbackMethod">Optional method you wish to callback if denied.</param>
        /// <param name="DenyCallbackParameters">Optional parameters for the <paramref name="DenyCallbackMethod"/>, must be set if <paramref name="DenyCallbackMethod"/> is set.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully. The Task holds the <c>string</c> token of the created <c>Proposal</c>.</returns>
        
        public async Task<Proposal> SendForAdminApproval(Action<Dictionary<string, string>> CallbackMethod,
                Dictionary<string, string> CallbackParameters, ulong Author, string Proposal, Action<Dictionary<string, string>> DenyCallbackMethod = null, Dictionary<string, string> DenyCallbackParameters = null) {

            string JSON = JsonConvert.SerializeObject(CallbackParameters);

            string DenyJSON = "";
            if(DenyCallbackParameters != null)
                DenyJSON = JsonConvert.SerializeObject(DenyCallbackParameters);

            return await ProposalService.SendAdminConfirmation(JSON, CallbackMethod.Target.GetType().Name,
                CallbackMethod.Method.Name, Author, Proposal, DenyJSON, DenyCallbackMethod != null ? DenyCallbackMethod.Target.GetType().Name : null,
                DenyCallbackMethod != null ? DenyCallbackMethod.Method.Name : null);
        }

        /// <summary>
        /// The Create Or Get Webhook finds the given channel and, when provided a name, attempts to find a webhook
        /// with that said name. If the webhook can not be found, it creates a new webhook in the channel with the set name.
        /// </summary>
        /// <param name="ChannelID">The Channel ID is the snowflake ID of the channel which you wish the webhook to be made in.</param>
        /// <param name="WebhookName">The Webhook Name is the identifier of the webhook, and is what the webhook will be called.</param>
        /// <returns>The DiscordWebhookClient of the webhook that has been gotten or created.</returns>

        public async Task<DiscordWebhookClient> CreateOrGetWebhook(ulong ChannelID, string WebhookName) {
            if (ChannelID <= 0)
                return null;

            SocketChannel Channel = DiscordSocketClient.GetChannel(ChannelID);

            if (Channel is SocketTextChannel TextChannel) {
                foreach (RestWebhook RestWebhook in await TextChannel.GetWebhooksAsync())
                    if (RestWebhook.Name.Equals(WebhookName))
                        return new DiscordWebhookClient(RestWebhook.Id, RestWebhook.Token);

                RestWebhook Webhook = await TextChannel.CreateWebhookAsync(WebhookName, ProfileService.GetRandomPFP());

                return new DiscordWebhookClient(Webhook.Id, Webhook.Token);
            }

            throw new Exception($"The webhook {WebhookName} could not be initialized in the given channel {Channel} due to it being of type {Channel.GetType().Name}.");
        }

        /// <summary>
        /// The Create Event Timer method is a generic method that will await for an expiration time to be reached
        /// before continuing execution of the code set in the CallbackMethod parameter.
        /// </summary>
        /// <param name="CallbackMethod">The method you wish to callback once expired.</param>
        /// <param name="CallbackParameters">The parameters you wish to callback with once expired.</param>
        /// <param name="SecondsTillExpiration">The count in seconds until the timer will expire.</param>
        /// <param name="TimerType">The given type of the timer, specifying if it should be removed after the set time (EXPIRE) or continue in the set interval.</param>
        /// <returns>The token associated with the timed event for future reference.</returns>

        public async Task<string> CreateEventTimer(Func<Dictionary<string, string>, Task> CallbackMethod,
                Dictionary<string, string> CallbackParameters, int SecondsTillExpiration, TimerType TimerType) {

            return await CreateEventTimer(CallbackMethod, CallbackParameters, SecondsTillExpiration, TimerType, TimerService);
        }

        /// <summary>
        /// The Create Event Timer method is a generic method that will await for an expiration time to be reached
        /// before continuing execution of the code set in the CallbackMethod parameter.
        /// </summary>
        /// <param name="CallbackMethod">The method you wish to callback once expired.</param>
        /// <param name="CallbackParameters">The parameters you wish to callback with once expired.</param>
        /// <param name="SecondsTillExpiration">The count in seconds until the timer will expire.</param>
        /// <param name="TimerType">The given type of the timer, specifying if it should be removed after the set time (EXPIRE) or continue in the set interval.</param>
        /// <param name="TimerService">The service used to access the database and related infrastructure to store the mute.</param>
        /// <returns>The token associated with the timed event for future reference.</returns>

        public static async Task<string> CreateEventTimer(Func<Dictionary<string, string>, Task> CallbackMethod,
                Dictionary<string, string> CallbackParameters, int SecondsTillExpiration, TimerType TimerType, TimerService TimerService)
        {

            string JSON = JsonConvert.SerializeObject(CallbackParameters);

            return await TimerService.AddTimer(JSON, CallbackMethod.Target.GetType().Name, CallbackMethod.Method.Name, SecondsTillExpiration, TimerType);
        }

        /// <summary>
        /// The Create Reaction Menu method creates a reaction menu with pages that you can use to navigate the embeds.
        /// </summary>
        /// <param name="EmbedBuilders">The embeds that you wish to create the reaction menu from.</param>
        /// <param name="Channel">The channel that the reaction menu should be sent to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task CreateReactionMenu(EmbedBuilder[] EmbedBuilders, ISocketMessageChannel Channel) {
            await ReactionMenuService.CreateReactionMenu(EmbedBuilders, Channel);
        }

    }

}
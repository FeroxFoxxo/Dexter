using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Events;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Newtonsoft.Json;

namespace Dexter.Abstractions
{

    /// <summary>
    /// The DiscordModule class is an abstract class all command modules extend upon.
    /// Command modules contain methods that run on the specified command being entered.
    /// </summary>

    public abstract class DiscordModule : ModuleBase<SocketCommandContext>
    {

        /// <summary>
        /// The ProposalService class is used to send a command to be accepted by an admin through the SendForAdminApproval method.
        /// </summary>
        public Proposals ProposalService { get; set; }

        /// <summary>
        /// The Interactivity class is used to create a reaction menu for the await CreateReactionMenu method.
        /// </summary>
        public InteractiveService Interactive { get; set; }

        /// <summary>
        /// The TimerService class is used to create a timer for wait until an expiration time has been reached.
        /// </summary>
        public Timers TimerService { get; set; }

        /// <summary>
        /// The ProfileService is used to find a random profile picture for a webhook on create or get.
        /// </summary>
        public Profiling ProfileService { get; set; }

        /// <summary>
        /// The BotConfiguration is used to find the thumbnails for the BuildEmbed method.
        /// </summary>
        public BotConfiguration BotConfiguration { get; set; }

        /// <summary>
        /// The DiscordShardedClient is used to create the webhook for the webhook on create or get.
        /// </summary>
        public DiscordShardedClient DiscordShardedClient { get; set; }

        /// <summary>
        /// The Build Embed method is a generic method that simply calls upon the EMBED BUILDER extension method.
        /// </summary>
        /// <param name="thumbnail">The thumbnail that you would like to be applied to the embed.</param>
        /// <returns>A new embed builder with the specified attributes applied to the embed.</returns>

        public EmbedBuilder BuildEmbed(EmojiEnum thumbnail)
        {
            return new EmbedBuilder().BuildEmbed(thumbnail, BotConfiguration, EmbedCallingType.Command);
        }

        /// <summary>
        /// The Send For Admin Approval method is a generic method that will send the related proposal to the administrators
        /// for approval. On approval it will callback the method specified with the given parameters.
        /// </summary>
        /// <param name="callbackMethod">The method you wish to callback once approved.</param>
        /// <param name="callbackParameters">The parameters you wish to callback with once approved.</param>
        /// <param name="author">The author of the message who will be attached to the proposal.</param>
        /// <param name="proposal">The message that will be attached to the proposal.</param>
        /// <param name="denyCallbackMethod">Optional method you wish to callback if denied.</param>
        /// <param name="denyCallbackParameters">Optional parameters for the <paramref name="denyCallbackMethod"/>, must be set if <paramref name="denyCallbackMethod"/> is set.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully. The Task holds the <c>string</c> token of the created <c>Proposal</c>.</returns>

        public async Task<Databases.Proposals.Proposal> SendForAdminApproval(Action<Dictionary<string, string>> callbackMethod,
                Dictionary<string, string> callbackParameters, ulong author, string proposal,
                Action<Dictionary<string, string>> denyCallbackMethod = null, Dictionary<string, string> denyCallbackParameters = null)
        {

            string json = JsonConvert.SerializeObject(callbackParameters);

            string denyJSON = "";

            if (denyCallbackParameters != null)
                denyJSON = JsonConvert.SerializeObject(denyCallbackParameters);

            return await ProposalService.SendAdminConfirmation(json, callbackMethod.Target.GetType().Name,
                callbackMethod.Method.Name, author, proposal, denyJSON, denyCallbackMethod?.Target.GetType().Name,
                denyCallbackMethod?.Method.Name);
        }

        /// <summary>
        /// The Create Or Get Webhook finds the given channel and, when provided a name, attempts to find a webhook
        /// with that said name. If the webhook can not be found, it creates a new webhook in the channel with the set name.
        /// </summary>
        /// <param name="channelID">The Channel ID is the snowflake ID of the channel which you wish the webhook to be made in.</param>
        /// <param name="webhookName">The Webhook Name is the identifier of the webhook, and is what the webhook will be called.</param>
        /// <returns>The DiscordWebhookClient of the webhook that has been gotten or created.</returns>

        public async Task<DiscordWebhookClient> CreateOrGetWebhook(ulong channelID, string webhookName)
        {
            if (channelID <= 0)
                return null;

            SocketChannel channel = DiscordShardedClient.GetChannel(channelID);

            if (channel is SocketTextChannel textChannel)
            {
                foreach (RestWebhook restWebhook in await textChannel.GetWebhooksAsync())
                    if (restWebhook.Name.Equals(webhookName))
                        return new DiscordWebhookClient(restWebhook.Id, restWebhook.Token);

                RestWebhook webhook = await textChannel.CreateWebhookAsync(webhookName, ProfileService.GetRandomPFP());

                return new DiscordWebhookClient(webhook.Id, webhook.Token);
            }

            throw new Exception($"The webhook {webhookName} could not be initialized in the given channel {channel} due to it being of type {channel.GetType().Name}.");
        }

        /// <summary>
        /// The Create Event Timer method is a generic method that will await for an expiration time to be reached
        /// before continuing execution of the code set in the CallbackMethod parameter.
        /// </summary>
        /// <param name="callbackMethod">The method you wish to callback once expired.</param>
        /// <param name="callbackParameters">The parameters you wish to callback with once expired.</param>
        /// <param name="secondsTillExpiration">The count in seconds until the timer will expire.</param>
        /// <param name="timerType">The given type of the timer, specifying if it should be removed after the set time (EXPIRE) or continue in the set interval.</param>
        /// <returns>The token associated with the timed event for future reference.</returns>

        public async Task<string> CreateEventTimer(Func<Dictionary<string, string>, Task> callbackMethod,
                Dictionary<string, string> callbackParameters, int secondsTillExpiration, TimerType timerType)
        {

            return await CreateEventTimer(callbackMethod, callbackParameters, secondsTillExpiration, timerType, TimerService);
        }

        /// <summary>
        /// The Create Event Timer method is a generic method that will await for an expiration time to be reached
        /// before continuing execution of the code set in the CallbackMethod parameter.
        /// </summary>
        /// <param name="callbackMethod">The method you wish to callback once expired.</param>
        /// <param name="callbackParameters">The parameters you wish to callback with once expired.</param>
        /// <param name="secondsTillExpiration">The count in seconds until the timer will expire.</param>
        /// <param name="timerType">The given type of the timer, specifying if it should be removed after the set time (EXPIRE) or continue in the set interval.</param>
        /// <param name="timerService">The service used to access the database and related infrastructure to store the mute.</param>
        /// <returns>The token associated with the timed event for future reference.</returns>

        public static async Task<string> CreateEventTimer(Func<Dictionary<string, string>, Task> callbackMethod,
                Dictionary<string, string> callbackParameters, int secondsTillExpiration, TimerType timerType, Timers timerService)
        {
            string json = JsonConvert.SerializeObject(callbackParameters);

            return await timerService.AddTimer(json, callbackMethod.Target.GetType().Name, callbackMethod.Method.Name, secondsTillExpiration, timerType);
        }

        /// <summary>
        /// The Create Reaction Menu method creates a reaction menu with pages that you can use to navigate the embeds.
        /// </summary>
        /// <param name="embeds">The embeds that you wish to create the reaction menu from.</param>
        /// <param name="channel">The channel that the reaction menu should be sent to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task CreateReactionMenu(EmbedBuilder[] embeds, IMessageChannel channel)
        {
            if (embeds.Length > 1)
            {
                PageBuilder[] pageBuilderMenu = new PageBuilder[embeds.Length];

                for (int i = 0; i < embeds.Length; i++)
                    pageBuilderMenu[i] = PageBuilder.FromEmbedBuilder(embeds[i]);

                Paginator paginator = new StaticPaginatorBuilder()
                    .WithPages(pageBuilderMenu)
                    .WithDefaultEmotes()
                    .WithFooter(PaginatorFooter.PageNumber)
                    .WithActionOnCancellation(ActionOnStop.DeleteInput)
                    .WithActionOnTimeout(ActionOnStop.DeleteInput)
                                        .Build();

                await Interactive.SendPaginatorAsync(paginator, channel, TimeSpan.FromMinutes(10));
            }
            else
                await embeds.FirstOrDefault().SendEmbed(Context.Channel);
        }

    }

}
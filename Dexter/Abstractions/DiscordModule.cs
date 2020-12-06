using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Services;
using Discord;
using Discord.Commands;
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
        /// The BotConfiguration is used to find the thumbnails for the BuildEmbed method.
        /// </summary>
        public BotConfiguration BotConfiguration { get; set; }

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
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task SendForAdminApproval(Func<Dictionary<string, string>, Task> CallbackMethod,
                Dictionary<string, string> CallbackParameters, ulong Author, string Proposal) {

            string JSON = JsonConvert.SerializeObject(CallbackParameters);

            await ProposalService.SendAdminConfirmation(JSON, CallbackMethod.Target.GetType().Name,
                CallbackMethod.Method.Name, Author, Proposal);
        }

        /// <summary>
        /// The Create Reaction Menu method creates a reaction menu with pages that you can use to navigate the embeds.
        /// </summary>
        /// <param name="EmbedBuilders">The embeds that you wish to create the reaction menu from.</param>
        /// <param name="Channel">The channel that the reaction menu should be sent to.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task CreateReactionMenu(EmbedBuilder[] EmbedBuilders, ISocketMessageChannel Channel) {
            await ReactionMenuService.CreateReactionMenu(EmbedBuilders, Channel);
        }

    }

}
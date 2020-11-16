using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
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
        /// The Build Embed method is a generic method that simply calls upon the EMBED BUILDER extension method.
        /// </summary>
        /// <param name="Thumbnail">The thumbnail that you would like to be applied to the embed.</param>
        /// <returns>A new embed builder with the specified attributes applied to the embed.</returns>
        public static EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail);
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
        public static async Task SendForAdminApproval(Func<Dictionary<string, string>, Task> CallbackMethod,
                Dictionary<string, string> CallbackParameters, ulong Author, string Proposal) {

            string JSON = JsonConvert.SerializeObject(CallbackParameters);

            await InitializeDependencies.ServiceProvider.GetRequiredService<ProposalService>().SendAdminConfirmation(JSON, CallbackMethod.Target.GetType().Name,
                CallbackMethod.Method.Name, Author, Proposal);
        }

    }

}

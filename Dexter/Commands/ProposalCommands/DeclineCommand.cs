using Dexter.Attributes.Methods;
using Dexter.Databases.Proposals;
using Dexter.Services;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ProposalCommands {

        /// <summary>
        /// The Decline Proposal method runs on the DECLINE command. It will change the proposal to an DECLINED status
        /// update the respective suggestion object connected to it.
        /// </summary>
        /// <param name="Tracker">The tracker is an alphanumeric identifier or the message ID of the suggestion.</param>
        /// <param name="Reason">The reason is the optional message that will be linked to the denial.
        /// This will be attached to the embed through the REASON field.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("decline")]
        [Summary("Denies a proposal from a tracker with an optional reason.")]
        [Alias("deny")]
        [RequireAdministrator]

        public async Task DeclineProposal(string Tracker, [Optional][Remainder] string Reason) {
            await ProposalService.EditProposal(Tracker, Reason, Context.User, Context.Channel, ProposalStatus.Declined);
        }

    }

}

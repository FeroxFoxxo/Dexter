using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Proposals;
using Dexter.Services;
using Discord.Commands;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;

namespace Dexter.Commands {

    public partial class ProposalCommands {

        /// <summary>
        /// The Fetch Proposal method runs on FETCH. It simply builds a proposal based on the tracker that is parsed to it.
        /// </summary>
        /// <param name="Tracker">The tracker the proposal is linked to, whether this be the alphanumeric token or the message ID.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("fetch")]
        [Summary("Fetches a proposal from the tracker or a message ID.")]
        [Alias("find")]
        [RequireModerator]

        public async Task FetchProposal(string Tracker) {
            Proposal Proposal = ProposalDB.GetProposalByNameOrID(Tracker);

            if (Proposal == null)
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Proposal does not exist!")
                    .WithDescription($"Cound not fetch the proposal from tracker / message ID / staff message ID `{Tracker}`.\n" +
                        $"Are you sure it exists?")
                    .SendEmbed(Context.Channel);
            else
                await ProposalService.BuildProposal(Proposal)
                    .SendEmbed(Context.Channel);
        }

    }

}
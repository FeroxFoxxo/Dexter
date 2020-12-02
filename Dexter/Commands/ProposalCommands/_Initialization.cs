using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Databases.Proposals;
using Dexter.Services;

namespace Dexter.Commands {

    /// <summary>
    /// The ProposalCommands module relates to the approval, denial and fetching of a suggested proposal.
    /// </summary>
    public partial class ProposalCommands : DiscordModule {

        private readonly ProposalDB ProposalDB;
        private readonly ProposalService ProposalService;

        /// <summary>
        /// The constructor for the ProposalCommands module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="ProposalDB">The ProposalDB stores the proposals that these commands interface.</param>
        /// <param name="ProposalService">The SuggestionService is the service for this related module,
        /// and is what is used to interface with the embeds after a suggestion has been approved.</param>
        public ProposalCommands(ProposalDB ProposalDB, ProposalService ProposalService) {
            this.ProposalDB = ProposalDB;
            this.ProposalService = ProposalService;
        }

    }

}

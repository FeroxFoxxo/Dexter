using Dexter.Abstractions;
using Dexter.Databases.Proposals;

namespace Dexter.Commands {

    /// <summary>
    /// The ProposalCommands module relates to the approval, denial and fetching of a suggested proposal.
    /// </summary>

    public partial class ProposalCommands : DiscordModule {

        /// <summary>
        /// The ProposalDB stores the proposals that these commands interface.
        /// </summary>
        
        public ProposalDB ProposalDB { get; set; }

    }

}

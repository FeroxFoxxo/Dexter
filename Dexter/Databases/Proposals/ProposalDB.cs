using Dexter.Abstractions;
using Dexter.Databases.AdminConfirmations;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.Proposals {

    /// <summary>
    /// The SuggestionDB contains a set of proposals that require confirmation stages.
    /// </summary>

    public class ProposalDB : Database {

        /// <summary>
        /// A table of the proposals issued in the SuggestionDB database.
        /// </summary>
        
        public DbSet<Proposal> Proposals { get; set; }

        /// <summary>
        /// A table of suggestions for voting in the SuggestionDB database.
        /// </summary>
        
        public DbSet<Suggestion> Suggestions { get; set; }

        /// <summary>
        /// A table of confirmations for admin approval in the SuggestionDB database.
        /// </summary>
        
        public DbSet<AdminConfirmation> AdminConfirmations { get; set; }

        /// <summary>
        /// The Get Proposal By Name Or ID method gets a proposal both from either its token, staff message ID or message ID.
        /// </summary>
        /// <param name="Tracker">The tracked variable of the proposal you would like to find.</param>
        /// <returns>A proposal object pertaining to the proposal that has been returned on the tracked token.</returns>
        
        public Proposal GetProposalByNameOrID(string Tracker) {
            Proposal TryTracked = Proposals.Find(Tracker);

            if (TryTracked != null)
                return TryTracked;

            if (ulong.TryParse(Tracker, out ulong TrackerASULONG)) {
                Proposal TryMessageID = Proposals.AsQueryable().Where(Proposal => Proposal.MessageID == TrackerASULONG).FirstOrDefault();

                if (TryMessageID != null)
                    return TryMessageID;

                Suggestion Suggestion = Suggestions.AsQueryable().Where(Suggestion => Suggestion.StaffMessageID == TrackerASULONG).FirstOrDefault();

                if (Suggestion != null)
                    return Proposals.Find(Suggestion.Tracker);
            }

            return null;
        }

    }

}

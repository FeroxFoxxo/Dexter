using System.Linq;
using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Proposals
{

    /// <summary>
    /// The SuggestionDB contains a set of proposals that require confirmation stages.
    /// </summary>

    public class ProposalDB : Database
	{

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
		/// <param name="tracker">The tracked variable of the proposal you would like to find.</param>
		/// <returns>A proposal object pertaining to the proposal that has been returned on the tracked token.</returns>

		public Proposal GetProposalByNameOrID(string tracker)
		{
			Proposal tryTracked = Proposals.Find(tracker);

			if (tryTracked != null)
            {
                return tryTracked;
            }

            if (ulong.TryParse(tracker, out ulong trackerAsULONG))
			{
				Proposal tryMessageID = Proposals.AsQueryable().Where(Proposal => Proposal.MessageID == trackerAsULONG).FirstOrDefault();

				if (tryMessageID != null)
                {
                    return tryMessageID;
                }

                Suggestion suggestion = Suggestions.AsQueryable().Where(Suggestion => Suggestion.StaffMessageID == trackerAsULONG).FirstOrDefault();

				if (suggestion != null)
                {
                    return Proposals.Find(suggestion.Tracker);
                }
            }

			return null;
		}

	}

}

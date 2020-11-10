using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.Suggestions {
    /// <summary>
    /// The SuggestionDB contains a set of suggestions proposed by a user through the #suggestion(s) channel.
    /// </summary>
    public class SuggestionDB : EntityDatabase {

        /// <summary>
        /// A table of the suggestions issued in the SuggestionDB database.
        /// </summary>
        public DbSet<Suggestion> Suggestions { get; set; }

        /// <summary>
        /// The Get Suggestion By Name Or ID method gets a suggestion both from either its token, staff message ID or message ID.
        /// </summary>
        /// <param name="Tracker">The tracked variable of the suggestion you would like to find.</param>
        /// <returns>A suggestion object pertaining to the suggestion that has been returned on the tracked token.</returns>
        public Suggestion GetSuggestionByNameOrID(string Tracker) {
            if (ulong.TryParse(Tracker, out ulong TrackerASULONG))
                return Suggestions.AsQueryable().Where(Suggestion => Suggestion.TrackerID == Tracker || Suggestion.StaffMessageID == TrackerASULONG || Suggestion.MessageID == TrackerASULONG).FirstOrDefault();
            else
                return Suggestions.AsQueryable().Where(Suggestion => Suggestion.TrackerID == Tracker).FirstOrDefault();
        }

    }
}

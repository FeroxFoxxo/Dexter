using Dexter.Abstractions;
using Dexter.Configurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Dexter.Databases.Suggestions {
    public class SuggestionDB : EntityDatabase {
        public DbSet<Suggestion> Suggestions { get; set; }

        public Suggestion GetSuggestionByNameOrID(string Tracker) {
            _ = ulong.TryParse(Tracker, out ulong TrackerASULONG);

            if (TrackerASULONG == 0)
                return Suggestions.AsQueryable().Where(Suggestion => Suggestion.TrackerID == Tracker).FirstOrDefault();
            else
                return Suggestions.AsQueryable().Where(Suggestion => Suggestion.TrackerID == Tracker || Suggestion.StaffMessageID == TrackerASULONG || Suggestion.MessageID == TrackerASULONG).FirstOrDefault();
        }
    }
}

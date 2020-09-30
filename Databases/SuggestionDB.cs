using Dexter.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Suggestions {
    public class SuggestionDB : EntityDatabase {
        public DbSet<Suggestion> Suggestions { get; set; }
    }

    public class Suggestion {
        public string Tracker;
        public string Suggestor;
        public string Status;
        public string Content;
        public string MessageID;
        public string StaffMessageID;
        public string Expiry;
    }
}

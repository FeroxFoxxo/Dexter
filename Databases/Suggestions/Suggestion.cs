using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Suggestions {
    public class Suggestion {
        [Key]
        public string TrackerID { get; set; }

        public ulong Suggestor { get; set; }

        public SuggestionStatus Status { get; set; }

        public string Content { get; set; }
        public string Reason { get; set; }

        public ulong MessageID { get; set; }
        public ulong StaffMessageID { get; set; }

        public string Expiry { get; set; }
    }
}

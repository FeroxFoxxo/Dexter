using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Warnings {
    public class Warning {

        [Key]
        public int WarningID { get; set; }

        public ulong Issuer { get; set; }
        public ulong User { get; set; }
        public string Reason { get; set; }
        public WarningType Type { get; set; }
        public long TimeOfIssue { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;
using Dexter.Enums;

namespace Dexter.Databases.FinalWarns
{
    /// <summary>
    /// Stores the information related to a final warning entry in a database, including the user it is attached to and whether it's active.
    /// </summary>

    public class FinalWarn {

        /// <summary>
        /// The unique ID for the user who received this final warning. This functions as a Key in the final warnings database.
        /// </summary>

        [Key]

        public ulong UserID { get; set; }

        /// <summary>
        /// Contains the unique user ID of the Staff member who issued the final warning.
        /// </summary>

        public ulong IssuerID { get; set; }

        /// <summary>
        /// The full reason for the associated final warn, detailing prior infractions and other related information.
        /// </summary>

        public string Reason { get; set; }

        /// <summary>
        /// The UNIX time the final warning was issued at.
        /// </summary>

        public long IssueTime { get; set; }

        /// <summary>
        /// The amount of time - in seconds - that the user was muted for immediately as a result of the final warning.
        /// </summary>

        public double MuteDuration { get; set; }

        /// <summary>
        /// Holds either EntryType.Issue - if the final warning is active - or EntryType.Revoke - if the final warning has been revoked.
        /// </summary>

        public EntryType EntryType { get; set; }

        /// <summary>
        /// The ID of the message within the final-warnings log channel that specifies the information about this final warning.
        /// </summary>

        public ulong MessageID { get; set; }

        /// <summary>
        /// The amount of Dexter Profile points deducted due to the infraction.
        /// </summary>

        public short PointsDeducted { get; set; }

    }
}

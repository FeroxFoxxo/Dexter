using Dexter.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Infractions {

    /// <summary>
    /// The Infraction class contains information on a warning, such as the ID, issuer, user and reason.
    /// It is stored in the InfractionsDB and can be pulled from its related Infractions DBSet.
    /// </summary>
    
    public class Infraction {

        /// <summary>
        /// The InfractionID is the KEY of the table.
        /// Every warning has a unique ID, and this increases on the amount of warnings filled.
        /// </summary>
        
        [Key]

        public int InfractionID { get; set; }

        /// <summary>
        /// The Issuer field is the snowflake ID of the moderator that has warned the user.
        /// </summary>
        
        public ulong Issuer { get; set; }

        /// <summary>
        /// The User field is the snowflake ID of the user that has been warned.
        /// </summary>
        
        public ulong User { get; set; }

        /// <summary>
        /// The Reason field is a description of what the warning was given for.
        /// </summary>
        
        public string Reason { get; set; }

        /// <summary>
        /// The EntryType field specifies if the warning is still valid or if it has been revoked.
        /// </summary>
        
        public EntryType EntryType { get; set; }

        /// <summary>
        /// The Time Of Issue field is a long field of the UNIX time at which the warning had been issued on.
        /// </summary>
        
        public long TimeOfIssue { get; set; }

        /// <summary>
        /// The amount of time associated with the infraction, in seconds.
        /// For mutes, this indicates the mute duration.
        /// </summary>
        /// <remarks>All warning-type infractions should have an InfractionTime of 0.</remarks>

        public int InfractionTime { get; set; }

        /// <summary>
        /// The amount of points subtracted from a user's Dexter Profile as a result of the infraction.
        /// </summary>

        public short PointCost { get; set; }

    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.GreetFur
{

    /// <summary>
    /// A record for a specific GreetFur and a specific day
    /// </summary>

    public class GreetFurRecord
    {
        /// <summary>
        /// Unique identifier for the record object.
        /// </summary>
        [Key]
        public uint RecordId { get; set; }

        /// <summary>
        /// Unique user identifier for the user this record refers to.
        /// </summary>
        public ulong UserID { get; set; }

        /// <summary>
        /// The amount of days since UNIX time before the day that this record represents.
        /// </summary>
        public int Date { get; set; }
        
        /// <summary>
        /// The amount of GreetFur-eligible messages sent by the user in the given day.
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Whether the user muted another user via the GreetFur-specific mute command in the selected day.
        /// </summary>
        public bool MutedUser { get; set; }
    }
}

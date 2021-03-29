using System;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.UserProfiles {

    /// <summary>
    /// Represents a recorded instance of a name change for a username.
    /// </summary>

    public class NameRecord {

        /// <summary>
        /// A unique identifier for the name record.
        /// </summary>

        [Key]
        public int Index { get; set; }

        /// <summary>
        /// The unique identifier of the user who used this name.
        /// </summary>

        public ulong UserID { get; set; }

        /// <summary>
        /// The Name being recorded.
        /// </summary>

        public string Name { get; set; }

        /// <summary>
        /// Represents the time the name was first set, in seconds since UNIX time.
        /// </summary>

        public long SetTime { get; set; }

        /// <summary>
        /// Whether the name record represents a USERNAME or a NICKNAME.
        /// </summary>

        public NameType Type { get; set; }

        /// <summary>
        /// Copies a record to avoid issues with deletion of data.
        /// </summary>

        public NameRecord Clone() {
            return new NameRecord() {
                Index = -1,
                Name = this.Name,
                UserID = this.UserID,
                SetTime = this.SetTime,
                Type = this.Type
            };
        }

        /// <summary>
        /// Returns the string expression of this name record.
        /// </summary>
        /// <returns>The name this record holds.</returns>

        public override string ToString() {
            return Name;
        }

        /// <summary>
        /// Formats the NameRecord as a listable expression with the time it was last used.
        /// </summary>
        /// <returns>A string containing the time the name was last used followed by the Name.</returns>

        public string Expression() {
            return $"{DateTimeOffset.FromUnixTimeSeconds(SetTime):MM/dd/yyy HH:mm 'UTC'z}: **{Name}**";
        }
    }
}
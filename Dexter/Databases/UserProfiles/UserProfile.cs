using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.UserProfiles {

    /// <summary>
    /// The Borkday class contains information on a user's last borkday.
    /// </summary>

    public class UserProfile {

        /// <summary>
        /// The UserID is the KEY of the table.
        /// It is the snowflake ID of the user that has had the borkday.
        /// </summary>

        [Key]

        public ulong UserID { get; set; }

        /// <summary>
        /// The UNIX time of when the borkday role was added last.
        /// </summary>

        public long BorkdayTime { get; set; }

        /// <summary>
        /// The time the user joined for the first time, expressed in seconds since UNIX time.
        /// </summary>

        public long DateJoined { get; set; }
    /*
        /// <summary>
        /// A collection of all nicknames a given user has used throughout their presence in the server.
        /// </summary>

        public DbSet<string> Nicknames { get; set; }

        /// <summary>
        /// A collection of all usernames (tags) a given user has used throughout their presence in the server.
        /// </summary>

        public DbSet<string> Usernames { get; set; }
    
        /// <summary>
        /// Contains stringified JSON information about Nicknames.
        /// </summary>

        public string NicknamesData { get; set; }

        /// <summary>
        /// Contains stringified JSON information about Usernames 
        /// </summary>

        public string UsernamesData { get; set; }
    */
    }

}

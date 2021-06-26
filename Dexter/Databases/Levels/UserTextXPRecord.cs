using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.Levels {

    /// <summary>
    /// Records whether a user has received XP in the last XP interval.
    /// </summary>

    public class UserTextXPRecord {
        /// <summary>
        /// The ID of the user who's being tracked
        /// </summary>
        public ulong Id { get; set; }
    }
}

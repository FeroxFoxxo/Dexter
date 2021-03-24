using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.UserRestrictions {
    
    /// <summary>
    /// Represents a set of restrictions that prevents a specific user from accessing certain Dexter Features.
    /// </summary>
    
    public class UserRestriction {

        /// <summary>
        /// The unique ID of the user this restriction affects.
        /// </summary>

        [Key]
        public ulong UserID { get; set; }

        /// <summary>
        /// The individual restrictions applied to the user this restriction represents.
        /// </summary>

        public Restriction RestrictionFlags { get; set; }

    }
}

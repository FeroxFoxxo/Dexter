using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.CommunityEvents {
    public enum EventType {

        /// <summary>
        /// An item that has been proposed by a user and is to be treated as a User-Hosted event.
        /// </summary>

        UserHosted,

        /// <summary>
        /// An item that has been proposed through the staff-exclusive method and is to be treated as an Official event.
        /// </summary>

        Official

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.Games {
    public class Player {

        [Key]
        public ulong UserID { get; set; }

        /// <summary>
        /// What game session the player is playing in.
        /// </summary>

        public int Playing { get; set; }

        public double Score { get; set; }

        public int Lives { get; set; }

        public string Data { get; set; }

    }
}

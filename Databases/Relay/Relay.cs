using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Relay {
    
    public class Relay {

        [Key]
        public ulong ChannelID { get; set; }

        public string Message { get; set; }

        public int MessageInterval { get; set; }

        public int CurrentMessageCount { get; set; }

    }

}

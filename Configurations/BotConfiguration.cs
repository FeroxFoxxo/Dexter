using Dexter.Core.Abstractions;
using System.Collections.Generic;

namespace Dexter.Core.Configuration {
    public class BotConfiguration : JSONConfiguration {

        public string Token { get; set; }

        public string Bot_Name { get; set; }

        public string Prefix { get; set; }

        public ulong ModeratorRoleID { get; set; }

        public ulong AdminitratorRoleID { get; set; }
        
        public Dictionary<string, ulong> ErrorChannel { get; set; }

        public string[] ThumbnailURLs { get; set; }

    }
}

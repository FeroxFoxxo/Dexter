using Dexter.Core.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configuration {
    public class BotConfiguration : JSONConfiguration {

        public string Token { get; set; }

        public string Bot_Name { get; set; }

        public string Prefix { get; set; }

        public ulong ModeratorRoleID { get; set; }

        public ulong GuildID { get; set; }

        public ulong ModerationLogChannelID { get; set; }

        public string[] ThumbnailURLs { get; set; }

        public string DeveloperMention { get; set; }

        public Dictionary<string, bool> ModuleConfigurations { get; set; }

        public bool EnableStartupAlert { get; set; }

    }
}

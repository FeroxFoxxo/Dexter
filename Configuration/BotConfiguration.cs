namespace Dexter.Core.Configuration {
    public class BotConfiguration : AbstractConfiguration {
        public string Token { get; set; }

        public string Bot_Name { get; set; }

        public ulong ModeratorRoleID { get; set; }

        public string[] ThumbnailURLs { get; set; }
    }

    public enum Thumbnails {
        Annoyed,
        Love,
        Null
    }
}

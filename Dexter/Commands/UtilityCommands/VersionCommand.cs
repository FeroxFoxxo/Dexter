using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Services;
using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Displays the current version Dexter is running on.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("version")]
        [Summary("Displays the current version Dexter is running on.")]
        [Alias("v")]

        public async Task VersionCommand() {
            using HttpClient HTTPClient = new();

            HTTPClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            using HttpResponseMessage Response = HTTPClient.GetAsync("https://api.github.com/repos/Frostrix/Dexter/commits").Result;
            string JSON = Response.Content.ReadAsStringAsync().Result;

            dynamic Commits = JArray.Parse(JSON);
            string LastCommit = Commits[0].commit.message;

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Bot Version")
                .WithDescription($"Hello? Is anyone out there-\nThis is **{Context.Client.CurrentUser.Username} v{StartupService.Version}** running **Discord.NET v{DiscordConfig.Version}**")
                .AddField("Latest Commit:", LastCommit.Length > 1200 ? $"{LastCommit.Substring(0, 1200)}..." : LastCommit)
                .SendEmbed(Context.Channel);
        }

    }

}

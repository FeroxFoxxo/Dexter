using Dexter.Attributes;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("logfile")]
        [Summary("Provides today's logfile.")]
        [Alias("log")]
        [RequireModerator]
        public async Task LogfileCommand() {
            string FilePath = LoggingService.LogFile;

            if (!File.Exists(FilePath))
                throw new FileNotFoundException();

            await Context.Channel.SendFileAsync(FilePath);
        }

    }
}

using Dexter.Attributes.Methods;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Sends a file containing all logged data by the current instance of the bot.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("logfile")]
        [Summary("Provides the logfile for the current instance of the bot.")]
        [Alias("log", "logs")]
        [RequireDeveloper]

        public async Task LogfileCommand() {
            if (!File.Exists(LoggingService.LogFile))
                throw new FileNotFoundException();

            await Context.Channel.SendFileAsync(LoggingService.LogFile);
        }

    }

}

using Dexter.Attributes.Methods;
using Dexter.Databases.FunTopics;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Returns a random topic from the database if the command provided is an empty string or null object.
        /// Otherwise, provides a list of options to suggest edits to the already-existing topics in the database.
        /// </summary>
        /// <remarks>This command has a 2-minute cooldown.</remarks>
        /// <param name="Command"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!\n" +
                    "`ADD [TOPIC]` - adds a topic to the database.\n" +
                    "`GET [TOPIC]` - gets a topic by name from the database.\n" +
                    "`EDIT [TOPIC ID] [TOPIC]` - edits a topic in the database.\n" +
                    "`REMOVE [TOPIC ID]` - removes a topic from the database.")]
        [CommandCooldown(120)]

        public async Task TopicCommand([Optional][Remainder] string Command) {
            await RunTopic(Command, TopicType.Topic);
        }

        /// <summary>
        /// Returns a random would-you-rather from the database if the command provided is an empty string or null object.
        /// Otherwise, provides a list of options to suggest edits to the already-existing would-you-rather questions in the database.
        /// </summary>
        /// <remarks>This command has a 2-minute cooldown.</remarks>
        /// <param name="Command"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.\n" +
                    "`ADD [WYR]` - adds a wyr to the database.\n" +
                    "`GET [WYR]` - gets a wyr by name from the database.\n" +
                    "`EDIT [WYR ID] [WYR]` - edits a wyr in the database.\n" +
                    "`REMOVE [WYR ID]` - removes a wyr from the database.")]
        [Alias("would you rather", "wouldyourather")]
        [CommandCooldown(120)]

        public async Task WYRCommand([Optional][Remainder] string Command) {
            await RunTopic(Command, TopicType.WouldYouRather);
        }

    }

}

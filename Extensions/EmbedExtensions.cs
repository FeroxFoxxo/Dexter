using Discord;
using Discord.Commands;
using Discord.Webhook;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Extensions {
    /// <summary>
    /// The Embed Extensions class offers a variety of different extensions that can be applied to an embed to modify or send it.
    /// </summary>
    public static class EmbedExtensions {

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IMessageChannel.
        /// </summary>
        /// <param name="Embed">The EmbedBuilder you wish to send.</param>
        /// <param name="Channel">The IMessageChannel you wish to send the embed to.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public static async Task SendEmbed(this EmbedBuilder Embed, IMessageChannel Channel) =>
            await Channel.SendMessageAsync(embed: Embed.Build());

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified DiscordWebhookClient channel.
        /// </summary>
        /// <param name="Embed">The EmbedBuilder you wish to send.</param>
        /// <param name="Channel">The DiscordWebhookClient you wish to send the embed to.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public static async Task SendEmbed(this EmbedBuilder Embed, DiscordWebhookClient Channel) =>
            await Channel.SendMessageAsync(embeds: new Embed[1] { Embed.Build() });

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IUser.
        /// </summary>
        /// <param name="Embed">The EmbedBuilder you wish to send.</param>
        /// <param name="User">The IUser you wish to send the embed to.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public static async Task SendEmbed(this EmbedBuilder Embed, IUser User) =>
            await User.SendMessageAsync(embed: Embed.Build());

        /// <summary>
        /// The AddField method adds a field to an EmbedBuilder if a given condition is true.
        /// </summary>
        /// <param name="Embed">The embed you wish to add the field to.</param>
        /// <param name="Condition">The condition which must be true to add the field.</param>
        /// <param name="Name">The name of the field you wish to add.</param>
        /// <param name="Value">The description of the field you wish to add.</param>
        /// <returns>The embed with the field added to it if true.</returns>
        public static EmbedBuilder AddField(this EmbedBuilder Embed, bool Condition, string Name, object Value) {
            if (Condition)
                Embed.AddField(Name, Value);

            return Embed;
        }

        /// <summary>
        /// The GetParametersForCommand adds fields to an embed containing the parameters and summary of the command.
        /// </summary>
        /// <param name="Embed">The Embed you wish to add the fields to.</param>
        /// <param name="CommandService">An instance of the CommandService, which contains all the currently active commands.</param>
        /// <param name="Command">The command of which you wish to search for.</param>
        /// <returns>The embed with the parameter fields for the command added.</returns>
        public static EmbedBuilder GetParametersForCommand(this EmbedBuilder Embed, CommandService CommandService, string Command) {
            SearchResult Result = CommandService.Search(Command);

            foreach (CommandMatch Match in Result.Commands) {
                CommandInfo CommandInfo = Match.Command;

                string CommandDescription = $"Parameters: {string.Join(", ", CommandInfo.Parameters.Select(p => p.Name))}";

                if (CommandInfo.Parameters.Count > 0)
                    CommandDescription = $"Parameters: {string.Join(", ", CommandInfo.Parameters.Select(p => p.Name))}";
                else
                    CommandDescription = "No parameters";

                if (!string.IsNullOrEmpty(CommandInfo.Summary))
                    CommandDescription += $"\nSummary: {CommandInfo.Summary}";

                Embed.AddField(string.Join(", ", CommandInfo.Aliases), CommandDescription);
            }

            return Embed;
        }
    }
}

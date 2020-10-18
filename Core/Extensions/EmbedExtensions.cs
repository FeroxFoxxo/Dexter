using Discord;
using Discord.Commands;
using Discord.Webhook;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Core.Extensions {
    public static class EmbedExtensions {
        public static async Task SendEmbed(this EmbedBuilder Embed, IMessageChannel Channel) =>
            await Channel.SendMessageAsync(embed: Embed.Build());

        public static async Task SendEmbed(this Embed Embed, IMessageChannel Channel) =>
            await Channel.SendMessageAsync(embed: Embed);

        public static async Task SendEmbed(this EmbedBuilder Embed, DiscordWebhookClient Channel) =>
            await Channel.SendMessageAsync(embeds: new Embed[1] { Embed.Build() });

        public static async Task SendEmbed(this EmbedBuilder Embed, IUser User) =>
            await User.SendMessageAsync(embed: Embed.Build());

        public static async Task SendEmbed(this Embed Embed, IUser User) =>
            await User.SendMessageAsync(embed: Embed);

        public static EmbedBuilder AddField(this EmbedBuilder Embed, bool Condition, string Name, object Value) {
            if (Condition)
                Embed.AddField(Name, Value);

            return Embed;
        }

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

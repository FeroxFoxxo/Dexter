using Dexter.Configuration;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Abstractions {
    public static class ExtensionMethods {
        public static async Task SendEmbed(this EmbedBuilder Embed, IMessageChannel Channel) =>
            await Channel.SendMessageAsync(embed: Embed.Build());

        public static async Task SendEmbed(this Embed Embed, IMessageChannel Channel) =>
            await Channel.SendMessageAsync(embed: Embed);

        public static async Task SendEmbed(this EmbedBuilder Embed, DiscordWebhookClient Channel) =>
            await Channel.SendMessageAsync(embeds: new Embed[1] { Embed.Build() });

        public static async Task SendEmbed(this EmbedBuilder Embed, SocketUser User) =>
            await User.SendMessageAsync(embed: Embed.Build());

        public static string Prettify(this string Name)
            => Regex.Replace(Name, @"(?<!^)(?=[A-Z])", " ");

        public static string Sanitize(this string Name)
            => Name.Replace("Commands", string.Empty);

        public static EmbedBuilder AddField(this EmbedBuilder Embed, bool Condition, string Name, object Value) {
            if (Condition)
                Embed.AddField(Name, Value);

            return Embed;
        }

        public static PermissionLevel GetPermissionLevel(this IGuildUser User, BotConfiguration Configuration) {
            if (User.GuildPermissions.Has(GuildPermission.Administrator))
                return PermissionLevel.Administrator;

            if (User.RoleIds.Contains(Configuration.ModeratorRoleID))
                return PermissionLevel.Moderator;

            return PermissionLevel.Default;
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

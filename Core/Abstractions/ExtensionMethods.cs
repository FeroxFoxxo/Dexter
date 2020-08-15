using Discord;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class ExtensionMethods {
        public static async Task SendEmbed(this EmbedBuilder Embed, IMessageChannel channel) {
            await channel.SendMessageAsync(string.Empty, false, Embed.Build());
        }

        public static EmbedBuilder AddField(this EmbedBuilder Embed, bool Condition, string Name, object Value) {
            if (Condition)
                Embed.AddField(Name, Value);

            return Embed;
        }
    }
}

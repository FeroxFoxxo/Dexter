using Discord;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class ExtensionMethods {
        public static async Task SendEmbed(this EmbedBuilder Embed, IMessageChannel channel)
            => await channel.SendMessageAsync(string.Empty, false, Embed.Build());
    }
}

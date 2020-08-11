using Discord;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class Extensions {
        public static async Task SendEmbed(this EmbedBuilder Embed, IMessageChannel channel)
            => await channel.SendMessageAsync(string.Empty, false, Embed.Build());
    }
}

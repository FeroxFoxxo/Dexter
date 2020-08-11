using Discord;
using Discord.Commands;

namespace Dexter.Core {
    public class Module : ModuleBase<SocketCommandContext> {
        protected static EmbedBuilder BuildEmbed() => new EmbedBuilder().WithColor(Color.Blue);
    }
}

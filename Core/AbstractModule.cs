using Discord;
using Discord.Commands;
using System;

namespace Dexter.Core {
    public class AbstractModule : ModuleBase<SocketCommandContext> {
        public static EmbedBuilder BuildEmbed() => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl("https://us-furries.com/Dexter/Dex" + (new Random().Next(3) == 0 ? "Wut" : new Random().Next(2) == 0 ? "Love" : "Annoyed") + ".png");
    }
}

using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;
using System;

namespace Dexter.Core {
    public class AbstractModule : ModuleBase<SocketCommandContext> {
        public static EmbedBuilder BuildEmbed(int ThumbnailURL) => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(((string[])JSONConfig.Get(typeof(BotConfiguration), "ThumbnailURLs")) [ ThumbnailURL ]);
    }
}

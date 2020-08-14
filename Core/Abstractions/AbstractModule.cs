using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;
using System;

namespace Dexter.Core {
    public class AbstractModule : ModuleBase<SocketCommandContext> {
        public static EmbedBuilder BuildEmbed() => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(((string[])JSONConfig.Get(typeof(BotConfiguration), "ThumbnailURLs")) [ new Random().Next(((string[]) JSONConfig.Get(typeof(BotConfiguration), "ThumbnailURLs")).Length - 1) ]);
    }
}

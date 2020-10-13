using Dexter.Abstractions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("emote")]
        [Summary("Gets the full image of an emote.")]
        [Alias("emoji")]
        public async Task EmojiCommand(string Emoji) {
            if (Emote.TryParse(Emoji, out var Emojis))
                await Context.BuildEmbed(EmojiEnum.Unknown)
                    .WithImageUrl(Emojis.Url)
                    .WithUrl(Emojis.Url)
                    .WithAuthor(Emojis.Name)
                    .WithTitle("Get Emoji URL")
                    .SendEmbed(Context.Channel);
            else
                await Context.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Emoji")
                    .WithDescription("An invalid emote was specified! Please make sure that what you have sent is a valid emote. Please make sure this is a **custom emote** aswell and does not fall under the unicode specification.")
                    .SendEmbed(Context.Channel);
        }

    }
}

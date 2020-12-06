using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        [Command("emote")]
        [Summary("Gets the full image of an emoji.")]
        [Alias("emoji")]

        public async Task EmojiCommand(string Emoji) {
            if (Emote.TryParse(Emoji, out Emote Emojis))
                await BuildEmbed(EmojiEnum.Unknown)
                    .WithImageUrl(Emojis.Url)
                    .WithUrl(Emojis.Url)
                    .WithAuthor(Emojis.Name)
                    .WithTitle("Get Emoji URL")
                    .SendEmbed(Context.Channel);
            else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Emoji")
                    .WithDescription("An invalid emoji was specified! Please make sure that what you have sent is a valid emoji. " +
                        "Please make sure this is a **custom emoji** aswell, and that it does not fall under the unicode specification.")
                    .SendEmbed(Context.Channel);
        }

    }

}

using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]

        public async Task AvatarCommand() {
            await AvatarCommand(Context.User);
        }

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]

        public async Task AvatarCommand(IUser User) {
            await BuildEmbed(EmojiEnum.Unknown)
                .WithImageUrl(User.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithUrl(User.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithAuthor(User)
                .WithTitle("Get Avatar URL")
                .SendEmbed(Context.Channel);
        }

    }
}

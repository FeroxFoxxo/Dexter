using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]
        public async Task AvatarCommand() {
            await AvatarCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]
        public async Task AvatarCommand(IGuildUser User) {
            await Context.BuildEmbed(EmojiEnum.Unknown)
                .WithImageUrl(User.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithUrl(User.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithAuthor(User)
                .WithTitle("Get Avatar URL")
                .SendEmbed(Context.Channel);
        }

    }
}

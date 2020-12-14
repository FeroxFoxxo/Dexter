using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]

        public async Task AvatarCommand([Optional] IUser User) {
            if (User == null)
                User = Context.User;

            await BuildEmbed(EmojiEnum.Unknown)
                .WithImageUrl(string.IsNullOrEmpty(User.GetAvatarUrl()) ? User.GetDefaultAvatarUrl() : User.GetAvatarUrl())
                .WithUrl(string.IsNullOrEmpty(User.GetAvatarUrl()) ? User.GetDefaultAvatarUrl() : User.GetAvatarUrl())
                .WithAuthor(User)
                .WithTitle("Get Avatar URL")
                .SendEmbed(Context.Channel);
        }

    }

}

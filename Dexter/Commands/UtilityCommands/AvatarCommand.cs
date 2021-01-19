using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Sends in the target user's profile picture as a full-resolution image. If no user is provided, defaults to Context.User.
        /// </summary>
        /// <param name="User">The target user, default to Context.User.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("avatar")]
        [Summary("Gets the avatar of a user mentioned or yours.")]

        public async Task AvatarCommand([Optional] IUser User) {
            if (User == null)
                User = Context.User;

            await BuildEmbed(EmojiEnum.Unknown)
                .WithImageUrl(User.GetTrueAvatarUrl(1024))
                .WithUrl(User.GetTrueAvatarUrl(1024))
                .WithAuthor(User)
                .WithTitle("Get Avatar URL")
                .SendEmbed(Context.Channel);
        }

    }

}

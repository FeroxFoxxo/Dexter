using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        /// <summary>
        /// Mutes a given user for a configured amount of time, if no user is specified, it defaults to Context.User.
        /// </summary>
        /// <remarks>
        /// Using this command on a target different from Context.User requires Staff permissions.
        /// This command has a 1-minute cooldown.
        /// </remarks>
        /// <param name="User">Optional parameter, indicates target user.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("muzzle")]
        [Summary("Issue the command, and s i l e n c e ,  T H O T-!")]
        [Alias("muzzleme")]
        [CommandCooldown(60)]

        public async Task MuzzleCommand([Optional] IGuildUser User) {
            bool IsUserSpecified = User != null;

            IGuildUser MuzzledUser = Context.Guild.GetUser(Context.User.Id);

            if (IsUserSpecified && (Context.User as IGuildUser).GetPermissionLevel(DiscordSocketClient, BotConfiguration) >= PermissionLevel.Moderator)
                MuzzledUser = User;
            
            await Muzzle(MuzzledUser);

            await Context.Channel.SendMessageAsync($"Muzzled **{MuzzledUser.Username}#{MuzzledUser.Discriminator}~!**");
        }

    }

}

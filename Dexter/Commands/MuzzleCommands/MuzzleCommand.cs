using Dexter.Attributes.Methods;
using Dexter.Attributes.Parameters;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        [Command("muzzle")]
        [Summary("Issue the command, and s i l e n c e ,  T H O T-!")]
        [Alias("muzzleme")]
        [CommandCooldown(60)]

        public async Task MuzzleCommand([Optional] IGuildUser User) {
            bool IsUserSpecified = User != null;

            IGuildUser MuzzledUser = Context.Guild.GetUser(Context.User.Id);

            if (IsUserSpecified && (Context.User as IGuildUser).GetPermissionLevel(BotConfiguration) >= PermissionLevel.Moderator)
                MuzzledUser = User;
            
            await Muzzle(MuzzledUser);

            await Context.Channel.SendMessageAsync($"Muzzled **{MuzzledUser.Username}#{MuzzledUser.Discriminator}~!**");
        }

    }

}

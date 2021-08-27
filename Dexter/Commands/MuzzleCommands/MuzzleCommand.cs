using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands
{

    public partial class MuzzleCommands
    {

        /// <summary>
        /// Mutes a given user for a configured amount of time, if no user is specified, it defaults to Context.User.
        /// </summary>
        /// <remarks>
        /// Using this command on a target different from Context.User requires Staff permissions.
        /// This command has a 1-minute cooldown.
        /// </remarks>
        /// <param name="args">Optional parameter, indicates target user.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("muzzle")]
        [Summary("Issue the command, and s i l e n c e ,  T H O T-!")]
        [Alias("muzzleme")]
        [CommandCooldown(60)]

        public async Task MuzzleCommand([Remainder] string args = "")
        {
            string argID = Regex.Match(args, @"[0-9]{18}").Value;

            ulong idToMuzzle;
            if (!string.IsNullOrEmpty(argID))
            {
                if (Context.User.GetPermissionLevel(DiscordSocketClient, BotConfiguration) < PermissionLevel.Moderator)
                {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Insufficient permissions")
                        .WithDescription("You aren't allowed to muzzle other users, you silly bean!")
                        .SendEmbed(Context.Channel);
                    return;
                }
                idToMuzzle = ulong.Parse(argID);
            }
            else
                idToMuzzle = Context.User.Id;

            IGuildUser toMuzzle = Context.Guild.GetUser(idToMuzzle);

            await Muzzle(toMuzzle);

            await Context.Channel.SendMessageAsync($"Muzzled **{toMuzzle.Username}#{toMuzzle.Discriminator}~!**");
        }

    }

}

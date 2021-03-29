using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        /// <summary>
        /// Unmuzzles a target user and notifies them of it.
        /// </summary>
        /// <remarks>This command is Staff-only.</remarks>
        /// <param name="User">The target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("unmuzzle")]
        [Summary("Unmuzzles the specified user.")]
        [RequireModerator]

        public async Task UnmuzzleCommand (IGuildUser User) {
            await Unmuzzle(User);

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Unmuzzled {User.Username}.")
                .WithDescription($"{User.Username} has successfully had their muzzle removed from them. Make sure to fed them with lots of pats! <3")
                .WithCurrentTimestamp()
                .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,
                    BuildEmbed(EmojiEnum.Love)
                        .WithTitle("You've Been Un-Muzzled!")
                        .WithDescription($"You have successfully been unmuzzled from **{Context.Guild.Name}**. Have a good one! <3")
                        .WithCurrentTimestamp()
            );
        }

    }

}

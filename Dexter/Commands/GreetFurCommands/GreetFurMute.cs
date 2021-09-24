using System;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{

    public partial class GreetFurCommands
    {

        /// <summary>
        /// The GreetFur mute command mutes users that do not have the Awoo role, and notify staff if they are Awoo.
        /// </summary>
        /// <param name="User">The user that the GreetFur has muted, as a Guild User instance.</param>
        /// <param name="Reason">The reason for the mute of the user, to be attached to the mute notification.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("gfmute")]
        [Summary("Mutes someone indefinitely and notifies the staff.")]
        [RequireGreetFur]

        public async Task GreetFurMute(IGuildUser User, [Remainder] string Reason)
        {
            if (User.RoleIds.Contains(GreetFurConfiguration.AwooRole))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Mute {User.Username}#{User.Discriminator}")
                    .WithDescription("This user is above the member role. As such, GreetFurs are unable to punish them. " +
                        "Please consult the GreetFur information channel for more information.")
                    .SendEmbed(Context.Channel);

                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle("Improper GreetFur Mute")
                    .WithDescription($"Hi, GreetFur {Context.User.GetUserInformation()} has tried run the ~gfmute command on {User.GetUserInformation()} for `{Reason}`. Probably best to keep an eye out for them...")
                    .SendEmbed(Context.Guild.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel);
            }
            else
            {
                await ModeratorCommands.MuteUser(User, TimeSpan.FromSeconds(0));

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Muted {User.Username}#{User.Discriminator}")
                    .WithDescription($"{User.GetUserInformation()} has been muted for `{Reason}`")
                    .SendEmbed(Context.Channel);

                await BuildEmbed(EmojiEnum.Unknown)
                    .WithAuthor(Context.User)
                    .AddField("User", $"<@{User.Id}>")
                    .AddField("GreetFur", $"<@{Context.User.Id}>")
                    .AddField("Reason", Reason)
                    .SendEmbed(DiscordSocketClient.GetChannel(GreetFurConfiguration.GreetFurMuteChannel) as ITextChannel);
            }
        }

    }

}

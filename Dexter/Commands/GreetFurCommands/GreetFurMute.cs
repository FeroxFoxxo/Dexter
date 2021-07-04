using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands
{

    public partial class GreetFurCommands
    {

        [Command("gfmute")]
        [Summary("Mutes someone indefinitely and notifies the staff.")]
        [RequireGreetFur]

        public async Task GreetFurMute (IGuildUser User, [Remainder] string Reason)
        {
            if (User.RoleIds.Contains(GreetFurConfiguration.AwooRole))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Mute {User.Username}#{User.Discriminator}")
                    .WithDescription("This user is above the member role. As such, GreetFurs are unable to punish them. " +
                        "Please consult the GreetFur information channel for more information.")
                    .WithCurrentTimestamp()
                    .WithFooter("USFurries Staff Team")
                    .SendEmbed(Context.Channel);

                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle("Improper GreetFur Mute")
                    .WithDescription($"Hi, GreetFur {Context.User.GetUserInformation()} has tried run the ~gfmute command on {User.GetUserInformation()} for `{Reason}`. Probably best to keep an eye out for them...")
                    .WithFooter("GreetFur Muting Module")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Guild.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel);
            } else
            {
                await ModeratorCommands.MuteUser(User, TimeSpan.FromSeconds(0));

            }
        }

    }

}

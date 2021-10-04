using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{

    public partial class UtilityCommands
    {

        /// <summary>
        /// Changes the bot's profile picture to a randomly selected image from a configured list.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("changepfp")]
        [Summary("Changes the profile picture of the bot to a random image from a selection made for him.")]
        [RequireModerator]

        public async Task ChangeProfile()
        {
            await DiscordShardedClient.CurrentUser
                .ModifyAsync(ClientProperties => ClientProperties.Avatar = new Image(ProfileService.GetRandomPFP()));

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Profile Changed.")
                .WithDescription($"Haiya! I've successfully changed my profile picture to {ProfileService.CurrentPFP}. " +
                    $"If this has been a delayed message, it may very well be due to ratelimiting! <3")
                .SendEmbed(Context.Channel);
        }

    }

}

using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        [Command("getpfp")]
        [Summary("Gets all the given profile pictures in the pfp directory.")]
        [Alias("getpfps")]

        public async Task GetProfiles () {
            string ProfilePictures = string.Join('\n', ProfileService.GetProfilePictures().Select(Profile => Profile.Name).ToArray());

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Profile Pictures:")
                .WithDescription(string.IsNullOrEmpty(ProfilePictures) ? "No Profile Pictures!" : ProfilePictures)
                .AddField(!string.IsNullOrEmpty(ProfileService.CurrentPFP), "Current PFP:", ProfileService.CurrentPFP)
                .SendEmbed(Context.Channel);
        }

    }

}

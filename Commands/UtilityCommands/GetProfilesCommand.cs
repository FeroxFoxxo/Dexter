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
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Profile Pictures.")
                .WithDescription($"Bellow lists the current profile pictures in the given profile picture directory.\n" +
                    $"{string.Join('\n', ProfileService.GetProfilePictures().Select(Profile => Profile.Name).ToArray())}")
                .AddField("Current PFP:", ProfileService.CurrentPFP)
                .SendEmbed(Context.Channel);
        }

    }
}

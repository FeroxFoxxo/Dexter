using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using Victoria.Node;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("queue")]
        [Summary("Displays the current queue of songs.")]
        public async Task QueueCommand()
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Could not display queue.")
                        .WithDescription(
                    "I couldn't find the music player for this server. " +
                    "Please ensure I am connected to a voice channel before using this command.").SendEmbed(Context.Channel);

                return;
            }

            var Embeds = player.GetQueue("🎶 Music Queue", BotConfiguration);

            if (Embeds.Length > 1)
                CreateReactionMenu(Embeds, Context.Channel);
            else
                await Embeds.FirstOrDefault().SendEmbed(Context.Channel);
        }

    }
}

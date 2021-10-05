using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Victoria.Node;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("volume")]
        [Summary("Changes the volume. Values are 0-150 and 100 is the default..")]
        [MusicBotChannel]
        [RequireDJ]

        public async Task VolumeCommand(int volumeLevel = 100)
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to change volume!")
                    .WithDescription("I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.")
                    .SendEmbed(Context.Channel);

                return;
            }

            try
            {
                int oldVolume = player.Volume;

                await player.SetVolumeAsync(volumeLevel);

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Volume changed.")
                    .WithDescription($"Sucessfully changed volume from {oldVolume} to {volumeLevel}").SendEmbed(Context.Channel);
            }
            catch (Exception)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to change volume!")
                    .WithDescription($"Failed to change volume to {volumeLevel}.\nIf the issue persists, please contact the developers for support.")
                    .SendEmbed(Context.Channel);

                Logger.LogError($"Failed to change volume in {Context.Guild.Id}.");

                return;
            }
        }
    }
}

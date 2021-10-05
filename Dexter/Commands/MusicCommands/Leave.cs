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

        [Command("leave")]
        [Summary("Disconnects me from the current voice channel.")]
        [MusicBotChannel]
        [RequireDJ]

        public async Task LeaveCommand()
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to leave VC!")
                    .WithDescription("I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.")
                    .SendEmbed(Context.Channel);

                return;
            }

            string vcName = $"**{player.VoiceChannel.Name}**";

            try
            {
                await LavaNode.LeaveAsync(player.VoiceChannel);

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Unable to leave VC!")
                    .WithDescription($"Disconnected from `{vcName}`.")
                    .SendEmbed(Context.Channel);
            }
            catch (Exception)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to leave VC!")
                    .WithDescription($"Failed to disconnect from {vcName}.\nIf the issue persists, please contact the developers for support.")
                    .SendEmbed(Context.Channel);

                Logger.LogError($"Failed to disconnect from voice channel '{vcName}' in {Context.Guild.Id} via $leave.");

                return;
            }
        }

    }
}

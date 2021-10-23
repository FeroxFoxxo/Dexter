using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
			if (!MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
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
				await MusicService.LavaNode.LeaveAsync(player.VoiceChannel);

				lock (MusicService.LoopLocker)
					if (MusicService.LoopedGuilds.ContainsKey(player.VoiceChannel.Guild.Id))
						MusicService.LoopedGuilds.Remove(player.VoiceChannel.Guild.Id);

				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Sucessfully left voice channel!")
					.WithDescription($"Disconnected from {vcName}.")
					.SendEmbed(Context.Channel);
			}
			catch (Exception)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to leave VC!")
					.WithDescription($"Failed to disconnect from {vcName}.\nIf the issue persists, please contact the developers for support.")
					.SendEmbed(Context.Channel);

				Logger.LogError($"Failed to disconnect from voice channel {vcName} in {Context.Guild.Id} via $leave.");

				return;
			}
		}

	}
}

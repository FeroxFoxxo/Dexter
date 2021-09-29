using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Discord;
using Discord.WebSocket;

namespace Dexter.Services
{

    /// <summary>
    /// The PrivateVCService deals with removing private VCs if nobody is in them.
    /// </summary>

    public class PrivateVCService : Service
    {

        /// <summary>
        /// The UtilityConfiguration instance for finding the private voice channel catagory.
        /// </summary>

        public UtilityConfiguration UtilityConfiguration { get; set; }

        /// <summary>
        /// The Initialize void hooks the Client.Ready event to the CheckRemoveVCs method.
        /// </summary>

        public override void Initialize()
        {
            DiscordShardedClient.ShardReady += (DiscordSocketClient _) => CheckRemoveVCs();
            DiscordShardedClient.UserVoiceStateUpdated += async (_, oldVoiceChannel, newVoiceChannel) => {
                if (oldVoiceChannel.VoiceChannel is not null
                && oldVoiceChannel.VoiceChannel.CategoryId == UtilityConfiguration.PrivateCategoryID)
                    await CheckRemoveVCs();
            };
        }

        /// <summary>
        /// This method checks through all channels to see if the channel is a private channel. If it is and nobody is in it, remove it! If all are removed, remove the lobby.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task CheckRemoveVCs()
        {
            if (DiscordShardedClient.GetChannel(UtilityConfiguration.PrivateCategoryID) is SocketCategoryChannel categoryChannel)
            {
                IEnumerable<SocketVoiceChannel> voiceChannels = categoryChannel.Guild.VoiceChannels.Where((SocketVoiceChannel check) => check.CategoryId == UtilityConfiguration.PrivateCategoryID && check.Name != UtilityConfiguration.WaitingVCName);

                bool voiceLobbyExists = false;

                foreach (SocketVoiceChannel voiceChannel in voiceChannels)
                {
                    int userCount = voiceChannel.Users.Count;

                    if (userCount <= 0)
                    {
                        await voiceChannel.DeleteAsync();
                    }
                    else
                        voiceLobbyExists = true;
                }

                if (!voiceLobbyExists)
                {
                    SocketVoiceChannel? waitingLobby = categoryChannel.Guild.VoiceChannels.FirstOrDefault((SocketVoiceChannel check) => check.Name == UtilityConfiguration.WaitingVCName);

                    if (waitingLobby != null)
                        await waitingLobby.DeleteAsync();
                }
            }
            else
            {
                await Debug.LogMessageAsync(
                    "Help! CategoryChannel is not set in the config files. Aborting!!",
                    LogSeverity.Error
                );
            }

        }

    }
}

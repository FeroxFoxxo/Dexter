using Dexter.Abstractions;
using Dexter.Configurations;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        /// Logs to the console if the voice channel does not exist.
        /// </summary>
        
        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// The Initialize void hooks the Client.Ready event to the CheckRemoveVCs method.
        /// </summary>

        public override void Initialize()
        {
            DiscordSocketClient.Ready += CheckRemoveVCs;
            DiscordSocketClient.UserVoiceStateUpdated += async (_, OldVoiceChannel, NewVoiceChannel) => {
                if (NewVoiceChannel.VoiceChannel == null && OldVoiceChannel.VoiceChannel != null)
                    if (OldVoiceChannel.VoiceChannel.CategoryId == UtilityConfiguration.PrivateCategoryID)
                        await CheckRemoveVCs();
            };
        }

        /// <summary>
        /// This method checks through all channels to see if the channel is a private channel. If it is and nobody is in it, remove it! If all are removed, remove the lobby.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task CheckRemoveVCs()
        {
            SocketCategoryChannel? CategoryChannel = DiscordSocketClient.GetChannel(UtilityConfiguration.PrivateCategoryID) as SocketCategoryChannel;

            if (CategoryChannel != null)
            {
                IEnumerable<SocketVoiceChannel> VoiceChannels = CategoryChannel.Guild.VoiceChannels.Where((SocketVoiceChannel Check) => Check.CategoryId == UtilityConfiguration.PrivateCategoryID && Check.Name != UtilityConfiguration.WaitingVCName);

                bool VoiceLobbyExists = false;

                foreach (SocketVoiceChannel VoiceChannel in VoiceChannels)
                {
                    int UserCount = VoiceChannel.Users.Count;

                    if (UserCount <= 0)
                    {
                        await VoiceChannel.DeleteAsync();
                    }
                    else
                        VoiceLobbyExists = true;
                }

                if (!VoiceLobbyExists)
                {
                    SocketVoiceChannel? WaitingLobby = CategoryChannel.Guild.VoiceChannels.FirstOrDefault((SocketVoiceChannel Check) => Check.Name == UtilityConfiguration.WaitingVCName);

                    if (WaitingLobby != null)
                        await WaitingLobby.DeleteAsync();
                }
            }
            else
            {
                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, "Private VC Service", "Help! CategoryChannel is not set in the config files. Aborting!!"));
            }

        }

    }
}

using Dexter.Abstractions;
using Dexter.Configurations;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
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
        /// The Initialize void hooks the Client.Ready event to the CheckRemoveVCs method.
        /// </summary>

        public override void Initialize()
        {
            DiscordSocketClient.Ready += LoopVCs;
            DiscordSocketClient.UserVoiceStateUpdated += async (_, OldVoiceChannel, NewVoiceChannel) => {
                if (NewVoiceChannel.VoiceChannel == null && OldVoiceChannel.VoiceChannel != null)
                    if (OldVoiceChannel.VoiceChannel.CategoryId == UtilityConfiguration.PrivateCategoryID)
                        await CheckRemoveVCs(OldVoiceChannel.VoiceChannel);
            };
        }

        private async Task LoopVCs()
        {
            SocketCategoryChannel CategoryChannel = DiscordSocketClient.GetChannel(UtilityConfiguration.PrivateCategoryID) as SocketCategoryChannel;

            foreach(SocketVoiceChannel Channel in CategoryChannel.Guild.VoiceChannels.Where( (IVoiceChannel Check ) => Check.CategoryId == CategoryChannel.Id))
            {
                await CheckRemoveVCs(Channel);
            }
        }

        private async Task CheckRemoveVCs(SocketVoiceChannel Channel)
        {
            int UserCount = Channel.Users.Count;

            if (UserCount <= 0)
            {
                IEnumerable<IVoiceChannel> VoiceChannels = Channel.Guild.VoiceChannels.Where((IVoiceChannel Check) => Check.CategoryId == UtilityConfiguration.PrivateCategoryID);
                
                if (VoiceChannels.Count() == 2)
                {
                    foreach (IVoiceChannel DeadChannel in VoiceChannels)
                    {
                        await DeadChannel.DeleteAsync();
                    }
                } else
                    await Channel.DeleteAsync();
            }
        }

    }
}

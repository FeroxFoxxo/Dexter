using Dexter.Abstractions;
using Dexter.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Victoria.Node;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands withing the Music module.
    /// </summary>

    public partial class MusicCommands : DiscordModule
    {

        public LavaNode LavaNode { get; set; }

        public MusicService AudioService { get; set; }

        public ILogger<MusicCommands> Logger { get; set; }

        public YouTubeService YouTubeService { get; set; }

        public ClientCredentialsRequest ClientCredentialsRequest { get; set; }

    }

}

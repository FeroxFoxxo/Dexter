using Dexter.Abstractions;
using Dexter.Configurations;
using DexterSlash.Services;
using SpotifyAPI.Web;
using System;
using Victoria.Node;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands withing the Music module.
    /// </summary>

    public partial class MusicCommands : DiscordModule
    {

        public LavaNode LavaNode { get; set; }

        public AudioService AudioService { get; set; }

        public SpotifyClient SpotifyAPI { get; set; }

    }

}

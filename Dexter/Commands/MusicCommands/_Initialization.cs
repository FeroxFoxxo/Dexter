using Dexter.Abstractions;
using DexterSlash.Services;
using Victoria.Node;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands withing the Music module.
    /// </summary>

    public partial class MusicCommands : DiscordModule
    {

        public LavaNode LavaNode { get; set; }

        public AudioService AudioService {  get; set; }

    }

}

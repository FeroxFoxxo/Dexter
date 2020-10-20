using Dexter.Configurations;
using Dexter.Core.Abstractions;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Dexter.Services {
    public class ModerationLogService : InitializableModule {

        private readonly ModerationConfiguration ModerationConfiguration;
        private readonly DiscordSocketClient DiscordSocketClient;

        public ModerationLogService(DiscordSocketClient _DiscordSocketClient, ModerationConfiguration _ModerationConfiguration) {
            ModerationConfiguration = _ModerationConfiguration;
            DiscordSocketClient = _DiscordSocketClient;
        }

        public override void AddDelegates() {
            DiscordSocketClient.ReactionRemoved += ReactionRemovedLog;
        }

        private async Task ReactionRemovedLog(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction) {
            
        }

    }
}

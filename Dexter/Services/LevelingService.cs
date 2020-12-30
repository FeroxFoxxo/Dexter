using Dexter.Abstractions;
using Dexter.Databases.Leveling;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Dexter.Services {
    /*
    public class LevelingService : Service {

        public LevellingDB LevellingDB { get; set; }

        public override void Initialize() {
            DiscordSocketClient.UserVoiceStateUpdated += VoiceUpdated;
            DiscordSocketClient.GuildMemberUpdated += MemberUpdated;
            DiscordSocketClient.Ready += Ready;
        }

        private async Task Ready() {
            foreach ()
        }

        private async Task MemberUpdated(SocketGuildUser From, SocketGuildUser To) {
            if (From.VoiceChannel == null && To.VoiceChannel != null)
                LevellingDB.VoiceLevels.Add(new VoiceLevel() { UserID = To.Id });
            else if (From.VoiceChannel != null && To.VoiceChannel == null) {
                VoiceLevel VoiceLevel = LevellingDB.VoiceLevels.Find(To.Id);
                if(VoiceLevel != null)
                    RemoveUser(VoiceLevel);
            }
        }

        private async Task RemoveUser(VoiceLevel VoiceLevel) {
            LevellingDB.VoiceLevels.Remove(VoiceLevel);
        }

        private async Task VoiceUpdated(SocketUser SocketUser, SocketVoiceState From, SocketVoiceState To) {

        }

    }
    */
}

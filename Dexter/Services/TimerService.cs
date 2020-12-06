using Dexter.Abstractions;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Dexter.Services {

    public class TimerService : Service {

        public TimerService(DiscordSocketClient DiscordSocketClient) {
            this.DiscordSocketClient = DiscordSocketClient;
        }

        public override void Initialize() {
            // Runs the bot timer to loop through all events that may occur on a timer.
            Timer EventTimer = new(TimeSpan.FromSeconds(20).TotalMilliseconds) {
                AutoReset = true,
                Enabled = true
            };

            EventTimer.Elapsed += (s, e) => LoopThroughEvents();

            DiscordSocketClient.Ready += () => Task.Run(() => EventTimer.Start());
        }

        public static void LoopThroughEvents() {

        }

    }

}

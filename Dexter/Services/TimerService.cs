using Dexter.Abstractions;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Dexter.Services {

    public class TimerService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;

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

        public void LoopThroughEvents() {

        }

    }

}

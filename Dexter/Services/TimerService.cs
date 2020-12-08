using Dexter.Abstractions;
using Dexter.Databases.EventTimers;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace Dexter.Services {

    public class TimerService : Service {

        public EventTimersDB EventTimersDB { get; set; }

        public LoggingService LoggingService { get; set; }

        public ServiceProvider ServiceProvider { get; set; }

        public override void Initialize() {
            // Runs the bot timer to loop through all events that may occur on a timer.
            Timer EventTimer = new (TimeSpan.FromSeconds(20).TotalMilliseconds) {
                AutoReset = true,
                Enabled = true
            };

            EventTimer.Elapsed += async (s, e) => await LoopThroughEvents();

            DiscordSocketClient.Ready += () => Task.Run(() => EventTimer.Start());
        }

        public async Task LoopThroughEvents() {
            long CurrentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.ExpirationTime <= CurrentTime)
                .ForEachAsync(async (EventTimer Timer) => {
                    EventTimersDB.EventTimers.Remove(Timer);

                    EventTimersDB.SaveChanges();

                    Dictionary<string, string> Parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Timer.CallbackParameters);
                    Type Class = Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.Name.Equals(Timer.CallbackClass)).FirstOrDefault();

                    if (Class.GetMethod(Timer.CallbackMethod) == null)
                        throw new NoNullAllowedException("The callback method specified for the admin confirmation is null! This could very well be due to the method being private.");

                    try {
                        await (Task) Class.GetMethod(Timer.CallbackMethod)
                            .Invoke(ServiceProvider.GetRequiredService(Class), new object[1] { Parameters });
                    } catch (Exception Exception) {
                        await LoggingService.LogMessageAsync(
                            new LogMessage(LogSeverity.Error, GetType().Name, Exception.Message, exception: Exception)
                        );
                    }
                });
        }

        public async Task AddTimer(string JSON, string ClassName, string MethodName, int SecondsTillExpiration) {
            EventTimersDB.EventTimers.Add(new EventTimer() {
                CallbackClass = ClassName,
                CallbackMethod = MethodName,
                CallbackParameters = JSON,
                ExpirationTime = SecondsTillExpiration + DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            EventTimersDB.SaveChanges();
        }

    }

}

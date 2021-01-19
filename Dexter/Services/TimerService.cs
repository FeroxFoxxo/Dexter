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

        /// <summary>
        /// The Random instance is used to pick a set number of random characters from the configuration to create a token.
        /// </summary>

        public Random Random { get; set; }

        public bool HasStarted = false;

        public override void Initialize() {
            DiscordSocketClient.Ready += HasTimerStarted;
        }

        public async Task HasTimerStarted () {
            // Runs the bot timer to loop through all events that may occur on a timer.
            if (!HasStarted) {
                Timer EventTimer = new(TimeSpan.FromSeconds(5).TotalMilliseconds) {
                    AutoReset = true,
                    Enabled = true
                };

                EventTimer.Elapsed += async (s, e) => await LoopThroughEvents();

                await Task.Run(() => EventTimer.Start());
                HasStarted = true;
            }
        }

        public async Task LoopThroughEvents() {
            long CurrentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.ExpirationTime <= CurrentTime)
                .ForEachAsync(async (EventTimer Timer) => {

                    switch (Timer.TimerType) {
                        case TimerType.Expire:
                            EventTimersDB.EventTimers.Remove(Timer);
                            break;
                        case TimerType.Interval:
                            Timer.ExpirationTime = Timer.ExpirationLength + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            break;
                    }

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

        public async Task<string> AddTimer(string JSON, string ClassName, string MethodName, int SecondsTillExpiration, TimerType TimerType) {
            string Token = CreateToken();

            EventTimer Timer = new () {
                Token = Token,
                ExpirationLength = SecondsTillExpiration,
                CallbackClass = ClassName,
                CallbackMethod = MethodName,
                CallbackParameters = JSON,
                ExpirationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (TimerType != TimerType.Interval ? SecondsTillExpiration : 0),
                TimerType = TimerType
            };

            EventTimersDB.EventTimers.Add(Timer);

            await EventTimersDB.SaveChangesAsync();

            return Timer.Token;
        }

        /// <summary>
        /// The Create Token method creates a random token for a timer.
        /// </summary>
        /// <returns>A randomly generated token in the form of a string that is not in the database already.</returns>

        public string CreateToken() {
            char[] TokenArray = new char[BotConfiguration.TrackerLength];

            for (int i = 0; i < TokenArray.Length; i++)
                TokenArray[i] = BotConfiguration.RandomCharacters[Random.Next(BotConfiguration.RandomCharacters.Length)];

            string Token = new(TokenArray);

            if (EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.Token == Token).FirstOrDefault() == null) {
                return Token;
            } else
                return CreateToken();
        }

        public bool TimerExists(string TimerTracker) {
            EventTimer Timer = EventTimersDB.EventTimers.Find(TimerTracker);

            if (Timer == null)
                return false;
            else
                return true;
        }

        public void RemoveTimer(string TimerTracker) {
            EventTimer Timer = EventTimersDB.EventTimers.Find(TimerTracker);

            if (Timer != null)
                EventTimersDB.EventTimers.Remove(Timer);

            EventTimersDB.SaveChanges();
        }

    }

}

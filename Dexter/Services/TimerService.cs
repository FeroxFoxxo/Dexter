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

    /// <summary>
    /// The Timer Service module loops through all the event timers in the EventTimerDatabase and runs them.
    /// Timers that expire will be removed but interval timers will loop at set intervals of time.
    /// </summary>

    public class TimerService : Service {

        /// <summary>
        /// The database holding all dynamic data to reference and operate with Event Timers.
        /// </summary>

        public EventTimersDB EventTimersDB { get; set; }

        /// <summary>
        /// The global service responsible for logging information into the DiscordSocketClient and console for debugging purposes.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// The ServiceProvider is where our dependencies are stored - used to execute an object when a timer is run.
        /// </summary>
        
        public ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The Random instance is used to pick a set number of random characters from the configuration to create a token.
        /// </summary>

        public Random Random { get; set; }

        /// <summary>
        /// <see langword="true"/> if the service has started; <see langword="false"/> otherwise.
        /// </summary>

        public bool HasStarted = false;

        /// <summary>
        /// The Initialize method hooks the client Ready events and begins to loop through all timers.
        /// </summary>
        
        public override void Initialize() {
            DiscordSocketClient.Ready += HasTimerStarted;
        }

        /// <summary>
        /// The Has Timer Started method loops through all timers for the set amount of seconds if there has not been a timer
        /// already created by creating a system timer that loops for a set amount of time.
        /// </summary>

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

        /// <summary>
        /// Loops through all events and checks if any of them are set to be triggered once (Expire) or periodically (Interval).
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

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

                    if (Class != null) {
                        if (Class.GetMethod(Timer.CallbackMethod) == null)
                            throw new NoNullAllowedException("The callback method specified for the admin confirmation is null! This could very well be due to the method being private.");

                        try {
                            await (Task)Class.GetMethod(Timer.CallbackMethod)
                                .Invoke(ServiceProvider.GetRequiredService(Class), new object[1] { Parameters });
                        } catch (Exception Exception) {
                            await LoggingService.LogMessageAsync(
                                new LogMessage(LogSeverity.Error, GetType().Name, Exception.Message, exception: Exception)
                            );
                        }
                    } else if (Timer.TimerType == TimerType.Interval)
                        EventTimersDB.EventTimers.Remove(Timer);
                });
        }

        /// <summary>
        /// Creates a new timer object and adds it to the corresponding database.
        /// </summary>
        /// <param name="JSON">JSON-formatted string-string Dictionary codifying the parameters to pass to <paramref name="ClassName"/>.<paramref name="MethodName"/>()</param>
        /// <param name="ClassName">The name of the class containing <paramref name="MethodName"/>.</param>
        /// <param name="MethodName">The name of the method to call when the event is triggered.</param>
        /// <param name="SecondsTillExpiration">Time until the event expires (Expire) or triggers (Interval).</param>
        /// <param name="TimerType">Whether the timer is a single-trigger timer (Expire) or triggers every set number of <paramref name="SecondsTillExpiration"/> (Interval).</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

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

        /// <summary>
        /// Checks whether a timer exists in the database from a given <paramref name="TimerTracker"/> Token.
        /// </summary>
        /// <param name="TimerTracker">The unique identifier of the target EventTimer.</param>
        /// <returns><see langword="true"/> if the timer exists; <see langword="false"/> otherwise.</returns>

        public bool TimerExists(string TimerTracker) {
            EventTimer Timer = EventTimersDB.EventTimers.Find(TimerTracker);

            if (Timer == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Removes a timer from the EventTimer database by its <paramref name="TimerTracker"/> token.
        /// </summary>
        /// <remarks>This function removes the timer given by its <paramref name="TimerTracker"/> from the database completely.</remarks>
        /// <param name="TimerTracker">The unique identifier of the target EventTimer.</param>

        public void RemoveTimer(string TimerTracker) {
            EventTimer Timer = EventTimersDB.EventTimers.Find(TimerTracker);

            if (Timer != null)
                EventTimersDB.EventTimers.Remove(Timer);

            EventTimersDB.SaveChanges();
        }

    }

}

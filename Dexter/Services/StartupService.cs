using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using Figgle;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Dexter.Services {

    /// <summary>
    /// The Startup Serivce module applies the token for the bot, as well as running the bot once all dependencies have loaded up.
    /// Furthermore, it logs and sends a message to the moderation channel when it does start up, including various information
    /// like its bot and Discord.NET versionings.
    /// </summary>
    
    public class StartupService : Service {

        /// <summary>
        /// An instance of the Logging Service, which we use to log if the token has not been set to the console.
        /// </summary>
        
        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// The ServiceProvider is where our dependencies are stored - given to get an initialized class.
        /// </summary>

        public ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The current version of the bot, which as been parsed from the InitializeDependencies method.
        /// </summary>

        public string Version;

        /// <summary>
        /// <see langword="true"/> if the bot has finished its startup process; <see langword="false"/> otherwise.
        /// </summary>

        public bool HasStarted = false;

        /// <summary>
        /// The Initialize method hooks the client ready event to the Display Startup Version Async method.
        /// </summary>
        
        public override void Initialize() {
            DiscordSocketClient.Ready += DisplayStartupVersionAsync;
        }

        /// <summary>
        /// The Start Async method runs at the end of the dependencies having been initialized,
        /// and is what runs the bot using the token and logs if the token doesn't exist to the console.
        /// </summary>
        /// <param name="Token">A string, containing the Token from the command line arguments.
        /// Returns false if does not exist and flows onto the token specified in the BotConfiguration.</param>
        /// <param name="Version">The current version of the bot, as parsed from the InitializeDependencies class.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task StartAsync(string Token, string Version) {
            this.Version = Version;

            if(!string.IsNullOrEmpty(Token))
                await RunBot(Token);
            else
                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, "Startup", $"The login token in the command line arguments was not set~!"));
        }

        /// <summary>
        /// The Run Bot method logs into the bot using the token specified as a parameter and then starts the bot asynchronously.
        /// </summary>
        /// <param name="Token">A string containing the token from which we use to log into Discord.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task RunBot(string Token) {
            LoggingService.LockedCMDOut = true;
            await DiscordSocketClient.LoginAsync(TokenType.Bot, Token);
            await DiscordSocketClient.StartAsync();
        }

        /// <summary>
        /// The Display Startup Version Async method runs on ready and is what attempts to log the initialization of the bot
        /// to a specified guild that the bot has sucessfully started and the versionings that it is running.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task DisplayStartupVersionAsync() {
            await DiscordSocketClient.SetActivityAsync(new Game(BotConfiguration.BotStatus));

            if (HasStarted)
                return;

            SocketChannel LoggingChannel = DiscordSocketClient.GetChannel(BotConfiguration.ModerationLogChannelID);

            Console.Clear();

            Console.Title = $"{DiscordSocketClient.CurrentUser.Username} v{Version} (Discord.Net v{DiscordConfig.Version})";

            Console.ForegroundColor = ConsoleColor.Cyan;

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(DiscordSocketClient.CurrentUser.Username));

            LoggingService.LockedCMDOut = false;

            if (LoggingChannel == null || LoggingChannel is not ITextChannel)
                return;

            Dictionary<string, List<string>> NulledConfigurations = new();

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfig)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Configuration => {
                    object Service = ServiceProvider.GetService(Configuration);

                    Configuration.GetProperties().ToList().ForEach(
                        Property => {
                            object Value = Property.GetValue(Service);

                            if (Value != null)
                                if (!string.IsNullOrEmpty(Value.ToString()) && !Value.ToString().Equals("0"))
                                    return;

                            if (!NulledConfigurations.ContainsKey(Configuration.Name))
                                NulledConfigurations.Add(Configuration.Name, new());

                            NulledConfigurations[Configuration.Name].Add(Property.Name);
                        }
                    );
                }
            );

            using HttpClient HTTPClient = new();

            HTTPClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            using HttpResponseMessage Response = HTTPClient.GetAsync(BotConfiguration.CommitAPICall).Result;
            string JSON = Response.Content.ReadAsStringAsync().Result;

            dynamic Commits = JArray.Parse(JSON);
            string LastCommit = Commits[0].commit.message;

            string UnsetConfigurations = string.Empty;

            foreach (KeyValuePair<string, List<string>> Configuration in NulledConfigurations)
                UnsetConfigurations += $"**{Configuration.Key} -** {string.Join(", ", Configuration.Value.Take(Configuration.Value.Count - 1)) + (Configuration.Value.Count > 1 ? " and " : "") + Configuration.Value.LastOrDefault()}\n";

            if (BotConfiguration.EnableStartupAlert || NulledConfigurations.Count > 0)
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Startup complete!")
                    .WithDescription($"This is **{DiscordSocketClient.CurrentUser.Username} v{Version}** running **Discord.Net v{DiscordConfig.Version}**!")
                    .AddField("Latest Commit:", LastCommit.Length > 1000 ? $"{LastCommit.Substring(0, 1000)}..." : LastCommit)
                    .AddField(NulledConfigurations.Count > 0, "Unapplied Configurations:", UnsetConfigurations.Length > 600 ? $"{UnsetConfigurations.Substring(0, 600)}..." : UnsetConfigurations)
                    .SendEmbed(LoggingChannel as ITextChannel);

            HasStarted = true;
        }

    }

}

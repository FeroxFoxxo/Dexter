using Dexter.Abstractions;
using Dexter.Configurations;
using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The Profile service deals with the modifying of the profile picture of the bot to a random
    /// profile picture selected in a given directory through the PFPConfiguration JSON file.
    /// </summary>
    public class ProfileService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly PFPConfiguration PFPConfiguration;
        private readonly LoggingService LoggingService;

        private readonly Random Random;

        /// <summary>
        /// The constructor for the ProfileService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// It also creates our instance of Random, which is used to pick a random file from the given directory.
        /// </summary>
        /// <param name="DiscordSocketClient">The DiscordSocketClient is used to hook the ready event up to the change PFP method.</param>
        /// <param name="PFPConfiguration">The PFPConfiguration stores data on where the pfps are located and which extensions are viable.</param>
        /// <param name="LoggingService">The LoggingService is used to log exceptions that occur on the attempt of trying to change a PFP.</param>
        public ProfileService(DiscordSocketClient DiscordSocketClient, PFPConfiguration PFPConfiguration,
                LoggingService LoggingService) {

            this.DiscordSocketClient = DiscordSocketClient;
            this.PFPConfiguration = PFPConfiguration;
            this.LoggingService = LoggingService;

            Random = new Random();
        }

        /// <summary>
        /// The AddDelegates void hooks the Client.Ready event to the ChangePFP method.
        /// </summary>
        public override void AddDelegates() {
            DiscordSocketClient.Ready += ChangePFP;
        }

        /// <summary>
        /// The ChangePFP method runs on Client.Ready and it simply changes the PFP of the bot.
        /// </summary>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task ChangePFP() {
            if (string.IsNullOrEmpty(PFPConfiguration.PFPDirectory))
                return;

            try {
                DirectoryInfo DirectoryInfo = new(PFPConfiguration.PFPDirectory);

                FileInfo[] Files = DirectoryInfo.GetFiles("*.*").Where(File => PFPConfiguration.PFPExtensions.Contains(File.Extension.ToLower())).ToArray();

                FileStream PFP = Files[Random.Next(0, Files.Length)].OpenRead();

                await DiscordSocketClient.CurrentUser.ModifyAsync(ClientProperties => ClientProperties.Avatar = new Image(PFP));
            } catch (Exception Exception) {
                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, GetType().Name, Exception.Message));
            }
        }
    }

}
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Enums;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The Profiling service deals with the modifying of the profile picture of the bot to a random
    /// profile picture selected in a given directory through the ProfilingConfiguration JSON file.
    /// It also deals with other profiling aspects like database saving.
    /// </summary>
    
    public class ProfilingService : Service {

        /// <summary>
        /// The ProfilingConfiguration stores data on where the pfps are located and which extensions are viable.
        /// </summary>
        
        public ProfilingConfiguration ProfilingConfiguration { get; set; }

        /// <summary>
        /// A reference to the global logging service used to log messages from both DiscordSocketClient and to the console for debugging purposes.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// The Random instance is used to pick a random file from the given directory
        /// </summary>

        public Random Random { get; set; }

        /// <summary>
        /// The CurrentPFP represents the filename of the current profile picture.
        /// </summary>

        public string CurrentPFP { get; private set; }

        /// <summary>
        /// The Initialize void hooks the Client.Ready event to the ChangePFP method.
        /// </summary>
        
        public override async void Initialize() {
            if (TimerService.EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.CallbackClass.Equals(GetType().Name)).FirstOrDefault() == null)
                await CreateEventTimer(ProfileCallback, new(), ProfilingConfiguration.SecTillProfiling, TimerType.Interval);
        }

        /// <summary>
        /// Randomly changes the bot's avatar, backs up the database to the configured channel, and measures the time elapsed in this process.
        /// </summary>
        /// <param name="Parameters">A string-string Dictionary containing no compulsory definitions.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task ProfileCallback(Dictionary<string, string> Parameters) {
            Stopwatch Stopwatch = new ();

            Stopwatch.Start();

            try {
                await DiscordSocketClient.CurrentUser.ModifyAsync(ClientProperties => ClientProperties.Avatar = new Image(GetRandomPFP()));
            } catch (HttpException) {
                await LoggingService.LogMessageAsync(
                    new LogMessage(LogSeverity.Warning, GetType().Name, "Unable to change the bot's profile picture due to ratelimiting!")
                );
            }

            if (BotConfiguration.EnableDatabaseBackups) {
                string DatabaseDirectory = Path.Join(Directory.GetCurrentDirectory(), "Databases");

                string BackupPath = Path.Join(Directory.GetCurrentDirectory(), "Backups");

                if (!Directory.Exists(BackupPath))
                    Directory.CreateDirectory(BackupPath);

                string BackupZip = Path.Join(BackupPath, $"{DateTime.UtcNow:dd-MM-yyyy}.zip");

                if (File.Exists(BackupZip))
                    File.Delete(BackupZip);

                ZipFile.CreateFromDirectory(DatabaseDirectory, BackupZip, CompressionLevel.Optimal, true);

                string[] Sizes = { "B", "KB", "MB", "GB", "TB" };

                double FileLength = new FileInfo(BackupZip).Length;

                int Order = 0;

                while (FileLength >= 1024 && Order < Sizes.Length - 1) {
                    Order++;
                    FileLength /= 1024;
                }

                Stopwatch.Stop();

                await (DiscordSocketClient.GetChannel(ProfilingConfiguration.DatabaseBackupChannel) as ITextChannel)
                    .SendFileAsync(BackupZip, embed:
                        BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Backup Successfully Concluded.")
                            .WithDescription($"Haiya! The backup for {DateTime.UtcNow.ToShortDateString()} has been built " +
                                $"with a file size of {string.Format("{0:0.##} {1}", FileLength, Sizes[Order])}.")
                            .WithCurrentTimestamp()
                            .WithFooter($"Profiling took {Stopwatch.Elapsed.Humanize()}")
                            .Build()
                    );
            }
        }

        /// <summary>
        /// The GetRandomPFP method runs on Client.Ready and it simply gets a random PFP of the bot.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public FileStream GetRandomPFP() {
            if (string.IsNullOrEmpty(ProfilingConfiguration.PFPDirectory))
                return null;

            FileInfo[] Files = GetProfilePictures();

            if (Files.Length <= 0)
                return null;

            else if (Files.Length == 1)
                return Files[0].OpenRead();

            else {
                FileInfo ProfilePicture = Files[Random.Next(0, Files.Length - 2)];

                if (ProfilePicture.Name == CurrentPFP)
                    ProfilePicture = Files[^1];

                CurrentPFP = ProfilePicture.Name;

                return ProfilePicture.OpenRead();
            }
        }

        /// <summary>
        /// The GetProfilePictures method runs on invocation and will get all the files in the pfp directory and return it. 
        /// </summary>
        /// <returns>A list of FileInfo's of each PFP in the given directory.</returns>
        
        public FileInfo[] GetProfilePictures() {
            DirectoryInfo DirectoryInfo = new(Path.Combine(Directory.GetCurrentDirectory(), ProfilingConfiguration.PFPDirectory));

            return DirectoryInfo.GetFiles("*.*").Where(File => ProfilingConfiguration.PFPExtensions.Contains(File.Extension.ToLower())).ToArray();
        }

    }

}
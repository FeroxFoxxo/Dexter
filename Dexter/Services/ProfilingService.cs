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
        
        public override void Initialize() {
            if (TimerService.EventTimersDB.EventTimers.AsQueryable().Where(Timer => Timer.CallbackClass.Equals(GetType().Name)).FirstOrDefault() == null)
                CreateEventTimer(ProfileCallback, new(), ProfilingConfiguration.SecTillProfiling, TimerType.Interval);
        }

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
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        public FileStream GetRandomPFP() {
            if (string.IsNullOrEmpty(ProfilingConfiguration.PFPDirectory))
                return null;

            FileInfo[] Files = GetProfilePictures();

            FileInfo ProfilePicture = Files[Random.Next(0, Files.Length)];

            CurrentPFP = ProfilePicture.Name;

            return ProfilePicture.OpenRead();
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
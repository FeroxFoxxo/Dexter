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
    
    public class ProfileService : Service {

        /// <summary>
        /// The PFPConfiguration stores data on where the pfps are located and which extensions are viable.
        /// </summary>
        
        public PFPConfiguration PFPConfiguration { get; set; }

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
            DiscordSocketClient.Ready += () =>
                Task.Run(async () =>
                    await DiscordSocketClient.CurrentUser.ModifyAsync(ClientProperties => ClientProperties.Avatar = new Image(GetRandomPFP())
                )
            );
        }

        /// <summary>
        /// The GetRandomPFP method runs on Client.Ready and it simply gets a random PFP of the bot.
        /// </summary>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public FileStream GetRandomPFP() {
            if (string.IsNullOrEmpty(PFPConfiguration.PFPDirectory))
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
            DirectoryInfo DirectoryInfo = new(Path.Combine(Directory.GetCurrentDirectory(), PFPConfiguration.PFPDirectory));

            return DirectoryInfo.GetFiles("*.*").Where(File => PFPConfiguration.PFPExtensions.Contains(File.Extension.ToLower())).ToArray();
        }

    }

}
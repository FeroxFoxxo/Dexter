using AnimatedGif;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Image = System.Drawing.Image;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Sends a specially generated animated emoji depicting a 'headpat' gif superposed over the target user's profile picture.
        /// </summary>
        /// <param name="User">The user whose profile picture is to be used as a base.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("headpat", RunMode = RunMode.Async)]
        [Summary("Ooh, you've been a good boy? *gives rapid headpats in an emoji*")]
        [Alias("headpats", "petpat", "petpats", "pet", "pat")]

        public async Task HeadpatCommand([Optional] IGuildUser User) {
            if (User == null)
                User = Context.Guild.GetUser(Context.User.Id);

            string NameOfUser = Regex.Replace(User.Username, "[^a-zA-Z]", "", RegexOptions.Compiled);

            if (NameOfUser.Length < 2)
                NameOfUser = "Unknown";

            string ImageCacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");

            if (!Directory.Exists(ImageCacheDir))
                Directory.CreateDirectory(ImageCacheDir);

            string FilePath = Path.Join(ImageCacheDir, $"{NameOfUser}.gif");

            using (AnimatedGifCreator Gif = AnimatedGif.AnimatedGif.Create(FilePath, 80)) {
                string[] Files = Directory.GetFiles(FunConfiguration.HeadpatsDir, "*.png", SearchOption.AllDirectories);

                using WebClient WebClient = new();
                using MemoryStream MemoryStream = new(WebClient.DownloadData(User.GetTrueAvatarUrl()));
                using Image PFPImage = Image.FromStream(MemoryStream);

                for (int Index = 0; Index < Files.Length; Index++) {
                    using Image Headpat = Image.FromFile(Files[Index]);

                    using Bitmap DrawnImage = new(Headpat.Width, Headpat.Height);

                    List<ushort> HeadpatPos = FunConfiguration.HeadpatPositions[Index];

                    using (Graphics Graphics = Graphics.FromImage(DrawnImage)) {
                        Graphics.DrawImage(PFPImage, HeadpatPos[0], HeadpatPos[1], HeadpatPos[2], HeadpatPos[3]);
                        Graphics.DrawImage(Headpat, 0, 0);
                    }

                    await Gif.AddFrameAsync(DrawnImage, delay: -1, quality: GifQuality.Bit8);
                }
            }

            using (Discord.Image EmoteImage = new (FilePath)) {
                IGuild Guild = DiscordSocketClient.GetGuild(FunConfiguration.HeadpatStorageGuild);

                Console.WriteLine(NameOfUser.Length);
                Console.WriteLine(EmoteImage);

                GuildEmote PrevEmote = Guild.Emotes.Where(Emote => Emote.Name == NameOfUser).FirstOrDefault();

                if (PrevEmote != null)
                    await Guild.DeleteEmoteAsync(PrevEmote);

                GuildEmote Emote = await Guild.CreateEmoteAsync(NameOfUser, EmoteImage);

                DiscordWebhookClient Webhook = await CreateOrGetWebhook(Context.Channel.Id, FunConfiguration.HeadpatWebhookName);

                await Webhook.SendMessageAsync(
                    Emote.ToString(),
                    username: string.IsNullOrEmpty(Context.Guild.GetUser(Context.User.Id).Nickname) ? Context.User.Username : Context.Guild.GetUser(Context.User.Id).Nickname,
                    avatarUrl: Context.User.GetTrueAvatarUrl()
                );

                await Guild.DeleteEmoteAsync(Emote);
            }

            File.Delete(FilePath);
        }

    }

}

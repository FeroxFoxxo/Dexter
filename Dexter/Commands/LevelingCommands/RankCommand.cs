using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {

    public partial class LevelingCommands {

        /// <summary>
        /// Displays the rank of a user given a user ID.
        /// </summary>
        /// <param name="userID">The user ID of the user to query.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("rank")]
        [Alias("level")]
        [BotChannel]

        public async Task RankCommand(ulong userID) {
            IUser user = DiscordSocketClient.GetUser(userID);

            if (user is null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find user!")
                    .WithDescription("This may be a caching issue, check that the ID is correct; if it is, try again later!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await RankCommand(user);
        }

        /// <summary>
        /// Displays the rank of a given user.
        /// </summary>
        /// <param name="user">The user to query the XP for.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>
        
        [Command("rank")]
        [Alias("level")]
        [Summary("Displays your Dexter level or that of other users.")]
        [BotChannel]

        public async Task RankCommand(IUser user = null) {
            if (user is null) {
                user = Context.User;
            }

            UserLevel ul = LevelingDB.GetOrCreateLevelData(user.Id, out LevelPreferences settings);

            int txtlvl = LevelingConfiguration.GetLevelFromXP(ul.TextXP, out long txtXP, out long txtXPLvl);
            int vclvl = LevelingConfiguration.GetLevelFromXP(ul.VoiceXP, out long vcXP, out long vcXPlvl);

            int totalLevel = ul.TotalLevel(LevelingConfiguration, txtlvl, vclvl);

            (await RenderRankCard(ul, settings)).Save(storagePath);
            await Context.Channel.SendFileAsync(storagePath);
            File.Delete(storagePath);
        }

        //Standard Item positioning
        private const int widthmain = 1000;
        private const int height = 450;
        private const int pfpside = 350;
        private const int levelWidth = 950;
        private const int levelHeight = 125;
        private const int defMargin = 25;
        private static readonly Rectangle mainRect = new Rectangle(0, 0, widthmain + pfpside, height);
        private static readonly Rectangle titleRect = new Rectangle(defMargin, defMargin, widthmain - 2 * defMargin + pfpside, labelHeight);
        private static readonly LevelRect mainLevel = new LevelRect(height - 2 * levelHeight - 2 * defMargin);
        private static readonly LevelRect secondaryLevel = new LevelRect(height - levelHeight - defMargin);
        private static readonly Rectangle rectName = new Rectangle(defMargin, defMargin, widthmain - 2 * defMargin + pfpside, labelHeight);
        private static readonly Rectangle rectPfp = new Rectangle(widthmain, height - pfpside , pfpside, pfpside);

        private const int miniLabelWidth = 80;
        private const int labelIntrusionPixels = 0;
        private const int labelHeight = 60;
        private const int typeLabelWidth = 175;
        private const int labelMiniMargin = 5;
        private static readonly Rectangle rectLevelLabel = new Rectangle(defMargin, defMargin, miniLabelWidth + labelIntrusionPixels, labelHeight);
        private static readonly Rectangle rectLevelText = new Rectangle(defMargin + miniLabelWidth, defMargin, widthmain / 2 - defMargin - miniLabelWidth, labelHeight);
        
        internal class LevelRect {
            public Rectangle fullRect;
            public Func<float, Rectangle> Bar;
            public Rectangle currentLevel;
            public Rectangle nextLevel;
            public Rectangle typeLabel;
            public Rectangle rankLabel;
            public Rectangle rankText;
            public Rectangle expText;

            public LevelRect(int originHeight) {
                fullRect = new Rectangle(defMargin, originHeight, levelWidth, levelHeight);
                Bar = (p) => new Rectangle(defMargin + barMarginHorizontal, originHeight + levelHeight - barHeight - barMarginVertical
                    , (int)((levelWidth - 2 * barMarginHorizontal) * p), barHeight);
                currentLevel = new Rectangle(defMargin, originHeight + levelHeight - barHeight - barMarginVertical, barMarginHorizontal, barHeight + 2 * barMarginVertical);
                nextLevel = new Rectangle(levelWidth + defMargin - barMarginHorizontal, originHeight + levelHeight - barHeight - barMarginVertical, barMarginHorizontal, barHeight + 2 * barMarginVertical);
                typeLabel = new Rectangle(defMargin + labelMiniMargin, originHeight + labelMiniMargin, typeLabelWidth, labelHeight);
                rankLabel = new Rectangle(defMargin + typeLabelWidth, originHeight, miniLabelWidth + labelIntrusionPixels, labelHeight);
                rankText = new Rectangle(defMargin + miniLabelWidth + typeLabelWidth, originHeight, levelWidth * 2 / 3 - miniLabelWidth - typeLabelWidth - defMargin, labelHeight);
                expText = new Rectangle(levelWidth / 3, originHeight, levelWidth * 2 / 3, labelHeight);
            }
        }

        internal class LevelData {
            public int level;
            public long rxp;
            public long lxp;
            public float Percent => rxp / (float)lxp;
            public string XpExpr => $"{rxp.ToUnit()} / {lxp.ToUnit()} XP";
            public string xpType;
            
            public int rank;

            public LevelRect rects;

            public LevelData(long xp, LevelRect rects, LevelingConfiguration config) {
                level = config.GetLevelFromXP(xp, out rxp, out lxp);
                this.rects = rects;
            }
        }

        //Level bars
        private const int barMarginVertical = 9;
        private const int barMarginHorizontal = 125;
        private const int barHeight = 75 - 2 * barMarginVertical;

        //Image paths
        private static readonly string imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "Levels");
        private static string BackgroundPath(string backgroundName = "default") {
            if (backgroundName.StartsWith("http"))
                throw new FileLoadException("File must be downloaded");         
            string[] extensions = new string[] { "jpg", "png" };
            string path = Path.Combine(imgPath, "Backgrounds");
            backgroundName = backgroundName.ToLower();
            foreach (string ext in extensions) {
                string filePath = Path.Combine(path, $"{backgroundName}.{ext}");
                if (File.Exists(filePath))
                    return filePath;
            }
            throw new FileNotFoundException($"No background file named {backgroundName}.");
        }
        private static readonly string barPath = Path.Combine(imgPath, "Assets", "LevelTemplate2.png");
        private static readonly string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache", "rankCard.png");

        private async Task<System.Drawing.Image> RenderRankCard(UserLevel ul, LevelPreferences settings) {
            Bitmap result = new(widthmain + pfpside, height);

            string fontPath = Path.Join(Directory.GetCurrentDirectory(), "Images", "OtherMedia", "Fonts", "Cuyabra", "cuyabra.otf");

            using PrivateFontCollection privateFontCollection = new();
            privateFontCollection.AddFontFile(fontPath);
            using FontFamily fontfamily = privateFontCollection.Families.First();

            using Font fontTitle = new(fontfamily, 55, GraphicsUnit.Pixel);
            using Font fontDefault = new(fontfamily, 40, GraphicsUnit.Pixel);
            using Font fontMini = new(fontfamily, 22, GraphicsUnit.Pixel);

            IUser user = DiscordSocketClient.GetUser(ul.UserID);
            if (user is null)
                throw new NullReferenceException($"Unable to obtain User from UserLevel object with ID {ul.UserID}.");

            LevelData mainLvlData;
            LevelData secondaryLvlData;
            int totallevel;

            string totallevelstr;

            if (LevelingConfiguration.LevelMergeMode is LevelMergeMode.AddXPMerged or LevelMergeMode.AddXPSimple) {
                mainLvlData = new(ul.TotalXP(LevelingConfiguration), mainLevel, LevelingConfiguration);
                secondaryLvlData = null;

                List<UserLevel> allUsers = LevelingDB.Levels.ToList();
                allUsers.Sort((a, b) => b.TotalXP(LevelingConfiguration).CompareTo(a.TotalXP(LevelingConfiguration)));
                mainLvlData.rank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;

                mainLvlData.xpType = "Level";
                totallevel = mainLvlData.level;
                totallevelstr = ul.TotalLevelStr(LevelingConfiguration);
            }
            else {
                mainLvlData = new(ul.TextXP > ul.VoiceXP ? ul.TextXP : ul.VoiceXP, mainLevel, LevelingConfiguration);
                secondaryLvlData = new(ul.TextXP > ul.VoiceXP ? ul.VoiceXP : ul.TextXP, secondaryLevel, LevelingConfiguration);
                
                List<UserLevel> allUsers = LevelingDB.Levels.ToList();
                allUsers.Sort((a, b) => b.TextXP.CompareTo(a.TextXP));
                int txtrank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;
                allUsers.Sort((a, b) => b.VoiceXP.CompareTo(a.VoiceXP));
                int vcrank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;

                if (ul.TextXP > ul.VoiceXP) {
                    mainLvlData.rank = txtrank;
                    mainLvlData.xpType = "Text";
                    secondaryLvlData.rank = vcrank;
                    secondaryLvlData.xpType = "Voice";
                }
                else {
                    mainLvlData.rank = vcrank;
                    mainLvlData.xpType = "Voice";
                    secondaryLvlData.rank = txtrank;
                    secondaryLvlData.xpType = "Text";
                }

                totallevel = ul.TotalLevel(LevelingConfiguration, mainLvlData.level, secondaryLvlData.level);
                totallevelstr = ul.TotalLevelStr(LevelingConfiguration, mainLvlData.level, secondaryLvlData.level);
            }


            using (Graphics g = Graphics.FromImage(result)) {
                try {
                    using System.Drawing.Image bg = System.Drawing.Image.FromFile(BackgroundPath(settings.Background ?? "default"));
                    g.DrawImage(bg, mainRect);
                }
                catch (FileNotFoundException) {
                    if (Regex.IsMatch(settings.Background, @"^(#|0x)?[0-9A-F]{6}$", RegexOptions.IgnoreCase)) {
                        System.Drawing.Color bgc = System.Drawing.Color.FromArgb(
                            unchecked((int)(0xff000000 + int.Parse(settings.Background[^6..], System.Globalization.NumberStyles.HexNumber))));
                        g.Clear(bgc);
                    }
                    else
                        g.Clear(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                }
                catch (FileLoadException) {
                    using WebClient client = new();
                    try {
                        byte[] dataArr = await client.DownloadDataTaskAsync(settings.Background);
                        using MemoryStream mem = new(dataArr);
                        using System.Drawing.Image bg = System.Drawing.Image.FromStream(mem);
                        g.DrawImage(bg, mainRect);
                    }
                    catch {
                        g.Clear(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                    }
                }

                using Brush xpColor = new SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                using Brush whiteColor = new SolidBrush(System.Drawing.Color.White);

                if (settings.TitleBackground)
                    g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0xd0, System.Drawing.Color.Black)), titleRect);
                g.DrawString("LEVEL", fontMini, xpColor, rectLevelLabel, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                g.DrawString(totallevel.ToString(), fontTitle, xpColor, rectLevelText, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
                SizeF offset = g.MeasureString(totallevel.ToString(), fontTitle);
                const int margin = 5;
                g.DrawString($"({totallevelstr})", fontDefault, xpColor
                    , new Rectangle(rectLevelText.X + (int)offset.Width + margin, rectLevelText.Y, widthmain / 2 - miniLabelWidth - margin - (int)offset.Width, labelHeight)
                    , new StringFormat { LineAlignment = StringAlignment.Far });
                                
                DrawLevels(fontTitle, fontDefault, fontMini, mainLvlData, secondaryLvlData, g, xpColor, whiteColor);

                const int pfpmargin = 3;
                if (settings.PfpBorder)
                    g.FillEllipse(new SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)0xff3f3f3f)))
                        , new Rectangle(rectPfp.X - pfpmargin, rectPfp.Y - pfpmargin, rectPfp.Width + 2 * pfpmargin, rectPfp.Height + 2 * pfpmargin));

                using (WebClient client = new()) {
                    byte[] dataArr = await client.DownloadDataTaskAsync(user.GetTrueAvatarUrl(512));
                    using MemoryStream mem = new(dataArr);
                    using System.Drawing.Image pfp = System.Drawing.Image.FromStream(mem);

                    if (settings.CropPfp) {
                        Rectangle tempPos = new Rectangle(Point.Empty, rectPfp.Size);
                        using Bitmap pfplayer = new Bitmap(rectPfp.Width, rectPfp.Height);
                        using Graphics pfpg = Graphics.FromImage(pfplayer);
                        using GraphicsPath path = new GraphicsPath();
                        path.AddEllipse(tempPos);
                        pfpg.Clip = new Region(path);
                        pfpg.DrawImage(pfp, tempPos);

                        g.DrawImage(pfplayer, rectPfp);
                    }
                    else {
                        g.DrawImage(pfp, rectPfp);
                    }
                }


                List<string> possibleNames = new();
                Font basicFont = new Font("Arial", labelHeight * 2 / 3);
                byte[] bytes = Encoding.UTF32.GetBytes(user.Username);
                string utf8encoded = Encoding.Default.GetString(bytes);
                StringBuilder excludenondrawables = new();
                StringBuilder simplifiedUsername = new();
                StringBuilder asciiUsername = new();
                foreach (char c in user.Username) {
                    try {
                        if (g.MeasureString(c.ToString(), fontDefault).Width > 2) {
                            excludenondrawables.Append(c);
                        }
                        else {
                            excludenondrawables.Append('?');
                        }
                    }
                    catch {
                        excludenondrawables.Append('?');
                    }

                    if (char.IsLetterOrDigit(c) || char.IsPunctuation(c)) simplifiedUsername.Append(c);
                    else simplifiedUsername.Append('?');

                    if (c < 256) asciiUsername.Append(c);
                    else asciiUsername.Append('?');
                }
                possibleNames.Add(utf8encoded.ToString());
                possibleNames.Add(excludenondrawables.ToString());
                possibleNames.Add(simplifiedUsername.ToString());
                possibleNames.Add(asciiUsername.ToString());
                possibleNames.Add("Unknown");

                foreach (string name in possibleNames) {
                    try {
                        Console.WriteLine($"Attempt to draw {name} with stylish font.");
                        Bitmap nameDrawn = new(rectName.Size.Width, rectName.Size.Height);
                        Console.WriteLine($"Is clear? {IsClearSafe(nameDrawn)}");
                        using Graphics gname = Graphics.FromImage(nameDrawn);
                        gname.DrawString($"{name}#{user.Discriminator}", fontDefault, whiteColor, new Rectangle(Point.Empty, rectName.Size),
                            new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap });
                        if (IsClearSafe(nameDrawn))
                            throw new Exception($"Unable to draw {name}");
                        g.DrawImage(nameDrawn, rectName);
                        break;
                    } catch {
                        continue;
                    }
                }
            }

            return result;
        }

        /* Unsafe, efficient version of IsClear in case unsafe code is enabled.
        private static bool IsClear(Bitmap bitmap) {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bool result = true;
            unsafe {
                PixelData* pPixel = (PixelData*)bitmapData.Scan0;
                for (int i = 0; i < bitmapData.Height && result; i++) {
                    for (int j = 0; j < bitmapData.Width; j++) {
                        if (pPixel->A != 0) {
                            result = false;
                            break;
                        }
                        pPixel++;
                    }
                    pPixel += bitmapData.Stride - (bitmapData.Width * 4);
                }
            }
            bitmap.UnlockBits(bitmapData);
            return result;
        }
        */

        private static bool IsClearSafe(Bitmap bitmap) {
            for (int i = 0; i < bitmap.Width; i++) {
                for (int j = 0; j < bitmap.Height; j++) {
                    if (bitmap.GetPixel(i, j).A != 0) return false;
                }
            }

            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PixelData      
        {                             
            public byte B;            
            public byte G;            
            public byte R;            
            public byte A;            
        }  

        private static void DrawLevels(Font fontTitle, Font fontDefault, Font fontMini, LevelData mainLvlData, LevelData secondaryLvlData, Graphics g, Brush xpColor, Brush whiteColor) {
            foreach (LevelData ld in new LevelData[] { mainLvlData, secondaryLvlData }) {
                if (ld is null) continue;
                g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0xe0, System.Drawing.Color.Black)), ld.rects.Bar(1));
                g.FillRectangle(xpColor, ld.rects.Bar(ld.Percent));
                using (System.Drawing.Image levelBox = System.Drawing.Image.FromFile(barPath)) {
                    g.DrawImage(levelBox, ld.rects.fullRect);
                }
                g.DrawString(ld.xpType, fontTitle, whiteColor, ld.rects.typeLabel);
                g.DrawString("RANK", fontMini, whiteColor, ld.rects.rankLabel, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                g.DrawString($"#{ld.rank}", fontTitle, whiteColor, ld.rects.rankText, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
                g.DrawString(ld.level.ToString(), fontTitle, xpColor, ld.rects.currentLevel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                g.DrawString((ld.level + 1).ToString(), fontTitle, xpColor, ld.rects.nextLevel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                g.DrawString(ld.XpExpr, fontDefault, xpColor, ld.rects.expText, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
            }
        }

        /// <summary>
        /// Runs a speed test on the calculation speed of inverse level calculations from XP.
        /// </summary>
        /// <returns></returns>

        [Command("testlevelcalculationspeed")]

        public async Task RunSpeedTest() {
            const int rep = 500000;
            for (int size = 1000; size < 100000000; size *= 10) {
                System.Diagnostics.Stopwatch t = new();
                t.Start();

                Random r = new();
                for (int i = 0; i < rep; i++) {
                    LevelingConfiguration.GetLevelFromXP(r.Next(0, size), out _, out _);
                }

                t.Stop();
                await Context.Channel.SendMessageAsync($"Calculation of {rep} level calculations completed in {t.ElapsedMilliseconds} milliseconds for size = {size}; level <= {LevelingConfiguration.GetLevelFromXP(size, out _, out _)}.");
            }
        }
    }
}

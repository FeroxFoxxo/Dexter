using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace Dexter.Commands
{

    public partial class LevelingCommands
    {

        /// <summary>
        /// Displays the rank of a user given a user ID.
        /// </summary>
        /// <param name="userID">The user ID of the user to query.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("rank")]
        [Alias("level")]
        [BotChannel]

        public async Task RankCommand(ulong userID)
        {
            IUser user = DiscordSocketClient.GetUser(userID);

            if (user is null)
            {
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

        public async Task RankCommand(IUser user = null)
        {
            if (user is null)
            {
                user = Context.User;
            }

            UserLevel ul = LevelingDB.GetOrCreateLevelData(user.Id, out LevelPreferences settings);
            int txtlvl = LevelingConfiguration.GetLevelFromXP(ul.TextXP, out long _, out long _);
            int vclvl = LevelingConfiguration.GetLevelFromXP(ul.VoiceXP, out long _, out long _);
            _ = ul.TotalLevel(LevelingConfiguration, txtlvl, vclvl);

            (await RenderRankCard(ul, settings)).Save(storagePath);
            await Context.Channel.SendFileAsync(storagePath);
            File.Delete(storagePath);
        }

        //Standard Item positioning
        /// <summary>
        /// The size of the rank card in pixels
        /// </summary>
        public static readonly Size RankCardSize = new(widthmain + pfpside, height);

        private const int widthmain = 1000;
        private const int height = 450;
        private const int pfpside = 350;
        private const int levelWidth = 950;
        private const int levelHeight = 125;
        private const int defMargin = 25;
        private static readonly Rectangle mainRect = new(0, 0, widthmain + pfpside, height);
        private static readonly Rectangle titleRect = new(defMargin, defMargin, widthmain - 2 * defMargin + pfpside, labelHeight);
        private static readonly LevelRect mainLevel = new(height - 2 * levelHeight - 2 * defMargin);
        private static readonly LevelRect secondaryLevel = new(height - levelHeight - defMargin);
        private static readonly LevelRect mainHybridLevel = new(height - levelHeight - defMargin, LevelRect.LevelBarType.HybridMain);
        private static readonly LevelRect secondaryHybridLevel = new(height - levelHeight - defMargin, LevelRect.LevelBarType.HybridSecondary);
        private static readonly Rectangle rectName = new(defMargin, defMargin, widthmain - 2 * defMargin + pfpside, labelHeight);
        private static readonly Rectangle rectPfp = new(widthmain, height - pfpside, pfpside, pfpside);

        private const int miniLabelWidth = 80;
        private const int labelIntrusionPixels = 0;
        private const int labelHeight = 60;
        private const int typeLabelWidth = 175;
        private const int hybridLabelWidth = 125;
        private const int labelMiniMargin = 10;
        private static readonly Rectangle rectLevelLabel = new(defMargin, defMargin, miniLabelWidth + labelIntrusionPixels, labelHeight);
        private static readonly Rectangle rectLevelText = new(defMargin + miniLabelWidth, defMargin, widthmain / 2 - defMargin - miniLabelWidth, labelHeight);

        internal class LevelRect
        {
            public Rectangle fullRect;
            public Func<float, Rectangle> Bar;
            public Rectangle currentLevel;
            public Rectangle nextLevel;
            public Rectangle typeLabel;
            public Rectangle rankLabel;
            public Rectangle rankText;
            public Rectangle expText;
            public LevelBarType leveltype;

            public LevelRect(int originHeight, LevelBarType leveltype = LevelBarType.Normal)
            {
                this.leveltype = leveltype;
                if (leveltype == LevelBarType.Normal)
                {
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
                else
                {
                    fullRect = new Rectangle(leveltype == LevelBarType.HybridMain ? defMargin : widthmain / 2 + labelMiniMargin, originHeight,
                        widthmain / 2 - labelMiniMargin - defMargin, levelHeight);
                    Bar = (p) => new Rectangle(fullRect.X + barMarginHorizontal, originHeight + levelHeight - barHeight - barMarginVertical
                        , (int)((fullRect.Width - barMarginHorizontal - labelMiniMargin) * p), barHeight);
                    currentLevel = new Rectangle(fullRect.X, originHeight + levelHeight - barHeight - barMarginVertical, barMarginHorizontal, barHeight + 2 * barMarginVertical);
                    nextLevel = default;
                    typeLabel = new Rectangle(fullRect.X + labelMiniMargin, originHeight + labelMiniMargin, hybridLabelWidth, labelHeight);
                    rankLabel = new Rectangle(fullRect.X, originHeight, fullRect.Width / 2 + labelIntrusionPixels, labelHeight);
                    rankText = new Rectangle(fullRect.X + fullRect.Width / 2, originHeight, fullRect.Width / 2, labelHeight);
                    expText = Bar(1);
                }
            }

            public enum LevelBarType
            {
                Normal,
                HybridMain,
                HybridSecondary
            }
        }

        internal class LevelData
        {
            public bool isHybrid = false;
            public int level;
            public long rxp;
            public long lxp;
            public float Percent => rxp / (float)lxp;
            public string XpExpr => $"{rxp.ToUnit()} / {lxp.ToUnit()}{(isHybrid ? "" : " XP")}";
            public string xpType;

            public int rank;

            public LevelRect rects;

            public LevelData(long xp, LevelRect rects, LevelingConfiguration config, bool isHybrid = false)
            {
                level = config.GetLevelFromXP(xp, out rxp, out lxp);
                this.rects = rects;
                this.isHybrid = isHybrid;
            }
        }

        //Level bars
        private const int barMarginVertical = 9;
        private const int barMarginHorizontal = 125;
        private const int barHeight = 75 - 3 * barMarginVertical;

        //Image paths
        private static readonly string imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "Levels");
        private static string BackgroundPath(string backgroundName = "default")
        {
            if (backgroundName.StartsWith("http"))
                throw new FileLoadException("File must be downloaded");
            string[] extensions = new string[] { "jpg", "png" };
            string path = Path.Combine(imgPath, "Backgrounds");
            backgroundName = backgroundName.ToLower();
            foreach (string ext in extensions)
            {
                string filePath = Path.Combine(path, $"{backgroundName}.{ext}");
                if (File.Exists(filePath))
                    return filePath;
            }
            throw new FileNotFoundException($"No background file named {backgroundName}.");
        }
        private static readonly string barPath = Path.Combine(imgPath, "Assets", "LevelTemplate2.png");
        private static readonly string hybridBarPath = Path.Combine(imgPath, "Assets", "LevelTemplateHybrid.png");
        private static readonly string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache", "rankCard.png");

        private async Task<System.Drawing.Image> RenderRankCard(UserLevel ul, LevelPreferences settings)
        {
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

            List<LevelData> levelsData = new();
            int totallevel;

            string totallevelstr;

            List<UserLevel> allUsers = LevelingDB.Levels.ToList();
            allUsers.Sort((a, b) => b.TextXP.CompareTo(a.TextXP));
            int txtrank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;
            allUsers.Sort((a, b) => b.VoiceXP.CompareTo(a.VoiceXP));
            int vcrank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;

            if (LevelingConfiguration.LevelMergeMode is LevelMergeMode.AddXPMerged or LevelMergeMode.AddXPSimple)
            {
                levelsData.Add(new LevelData(ul.TotalXP(LevelingConfiguration), mainLevel, LevelingConfiguration));

                allUsers.Sort((a, b) => b.TotalXP(LevelingConfiguration).CompareTo(a.TotalXP(LevelingConfiguration)));
                levelsData[0].rank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;

                levelsData[0].xpType = "Level";
                totallevel = levelsData[0].level;
                totallevelstr = ul.TotalLevelStr(LevelingConfiguration);

                if (settings.ShowHybrid)
                {
                    levelsData.Add(new LevelData(ul.TextXP > ul.VoiceXP ? ul.TextXP : ul.VoiceXP, mainHybridLevel, LevelingConfiguration, true));
                    levelsData.Add(new LevelData(ul.TextXP > ul.VoiceXP ? ul.VoiceXP : ul.TextXP, secondaryHybridLevel, LevelingConfiguration, true));

                    if (ul.TextXP > ul.VoiceXP)
                    {
                        levelsData[1].rank = txtrank;
                        levelsData[1].xpType = "Txt";
                        levelsData[2].rank = vcrank;
                        levelsData[2].xpType = "VC";
                    }
                    else
                    {
                        levelsData[1].rank = vcrank;
                        levelsData[1].xpType = "VC";
                        levelsData[2].rank = txtrank;
                        levelsData[2].xpType = "Txt";
                    }
                }
            }
            else
            {
                levelsData.Add(new LevelData(ul.TextXP > ul.VoiceXP ? ul.TextXP : ul.VoiceXP, mainLevel, LevelingConfiguration));
                levelsData.Add(new LevelData(ul.TextXP > ul.VoiceXP ? ul.VoiceXP : ul.TextXP, secondaryLevel, LevelingConfiguration));

                if (ul.TextXP > ul.VoiceXP)
                {
                    levelsData[0].rank = txtrank;
                    levelsData[0].xpType = "Text";
                    levelsData[1].rank = vcrank;
                    levelsData[1].xpType = "Voice";
                }
                else
                {
                    levelsData[0].rank = vcrank;
                    levelsData[0].xpType = "Voice";
                    levelsData[1].rank = txtrank;
                    levelsData[1].xpType = "Text";
                }

                totallevel = ul.TotalLevel(LevelingConfiguration, levelsData[0].level, levelsData[1].level);
                totallevelstr = ul.TotalLevelStr(LevelingConfiguration, levelsData[0].level, levelsData[1].level);
            }


            using (Graphics g = Graphics.FromImage(result))
            {
                try
                {
                    using System.Drawing.Image bg = System.Drawing.Image.FromFile(BackgroundPath(settings.Background ?? "default"));
                    g.DrawImage(bg, mainRect);
                }
                catch (FileNotFoundException)
                {
                    if (Regex.IsMatch(settings.Background, @"^(#|0x)?[0-9A-F]{6}$", RegexOptions.IgnoreCase))
                    {
                        System.Drawing.Color bgc = System.Drawing.Color.FromArgb(
                            unchecked((int)(0xff000000 + int.Parse(settings.Background[^6..], System.Globalization.NumberStyles.HexNumber))));
                        g.Clear(bgc);
                    }
                    else
                        g.Clear(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                }
                catch (FileLoadException)
                {
                    using HttpClient client = new();
                    try
                    {
                        byte[] dataArr = await client.GetByteArrayAsync(settings.Background);
                        using MemoryStream mem = new(dataArr);
                        using System.Drawing.Image bg = System.Drawing.Image.FromStream(mem);
                        g.DrawImage(bg, mainRect);
                    }
                    catch
                    {
                        g.Clear(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                    }
                }

                using SolidBrush xpColor = new(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                using SolidBrush whiteColor = new(System.Drawing.Color.White);

                if (settings.TitleBackground)
                    g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0xd0, System.Drawing.Color.Black)), titleRect);
                g.DrawString("LEVEL", fontMini, xpColor, rectLevelLabel, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                g.DrawString(totallevel.ToString(), fontTitle, xpColor, rectLevelText, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
                SizeF offset = g.MeasureString(totallevel.ToString(), fontTitle);
                const int margin = 5;
                g.DrawString($"({totallevelstr})", fontDefault, xpColor
                    , new Rectangle(rectLevelText.X + (int)offset.Width + margin, rectLevelText.Y, widthmain / 2 - miniLabelWidth - margin - (int)offset.Width, labelHeight)
                    , new StringFormat { LineAlignment = StringAlignment.Far });

                DrawLevels(fontTitle, fontDefault, fontMini, levelsData, g, xpColor, whiteColor, settings);

                const int pfpmargin = 3;
                if (settings.PfpBorder)
                    g.FillEllipse(new SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)0xff3f3f3f)))
                        , new Rectangle(rectPfp.X - pfpmargin, rectPfp.Y - pfpmargin, rectPfp.Width + 2 * pfpmargin, rectPfp.Height + 2 * pfpmargin));

                using (HttpClient client = new())
                {
                    try
                    {
                        byte[] dataArr = await client.GetByteArrayAsync(user.GetTrueAvatarUrl(512));
                        using MemoryStream mem = new(dataArr);
                        using System.Drawing.Image pfp = System.Drawing.Image.FromStream(mem);

                        if (settings.CropPfp)
                        {
                            Rectangle tempPos = new(Point.Empty, rectPfp.Size);
                            using Bitmap pfplayer = new(rectPfp.Width, rectPfp.Height);
                            using Graphics pfpg = Graphics.FromImage(pfplayer);
                            using GraphicsPath path = new();
                            path.AddEllipse(tempPos);
                            pfpg.Clip = new Region(path);
                            pfpg.DrawImage(pfp, tempPos);

                            g.DrawImage(pfplayer, rectPfp);
                        }
                        else
                        {
                            g.DrawImage(pfp, rectPfp);
                        }
                    }
                    catch (HttpRequestException)
                    {
                        g.DrawEllipse(new Pen(xpColor), rectPfp);
                    }
                }

                List<string> possibleNames = new();
                Font basicFont = new("Arial", labelHeight * 2 / 3);
                StringBuilder simplifiedUsername = new();
                StringBuilder asciiUsername = new();
                foreach (char c in user.Username)
                {

                    if (char.IsLetterOrDigit(c) || char.IsPunctuation(c)) simplifiedUsername.Append(c);
                    else simplifiedUsername.Append('?');

                    if (c < 256) asciiUsername.Append(c);
                    else asciiUsername.Append('?');
                }
                possibleNames.Add(user.Username);
                possibleNames.Add(simplifiedUsername.ToString());
                possibleNames.Add(asciiUsername.ToString());
                possibleNames.Add("Unknown");

                foreach (string name in possibleNames)
                {
                    try
                    {
                        Bitmap nameDrawn = new(rectName.Size.Width, rectName.Size.Height);
                        using Graphics gname = Graphics.FromImage(nameDrawn);
                        gname.DrawString($"{name}#{user.Discriminator}", fontDefault, whiteColor, new Rectangle(Point.Empty, rectName.Size),
                            new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
                        if (IsClearSafe(nameDrawn, out Point loc))
                            throw new Exception($"Unable to draw {name}");
                        g.DrawImage(nameDrawn, rectName);
                        break;
                    }
                    catch
                    {
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

        private static bool IsClearSafe(Bitmap bitmap, out Point location)
        {
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    if (bitmap.GetPixel(i, j).A != 0)
                    {
                        Console.WriteLine($"Found pixel ({i},{j}) with color {bitmap.GetPixel(i, j).ToArgb():X} (dimensions: {bitmap.Width}x{bitmap.Height})");
                        location = new Point(i, j);
                        return false;
                    }
                }
            }

            location = Point.Empty;
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

        private static void DrawLevels(Font fontTitle, Font fontDefault, Font fontMini, IEnumerable<LevelData> levels, Graphics g, SolidBrush xpColor, SolidBrush whiteColor, LevelPreferences prefs)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (LevelData ld in levels)
            {
                if (ld is null) continue;
                Rectangle barRect = ld.rects.Bar(1);
                GraphicsPath barGPath = GraphicsExtensions.RoundedRect(barRect, barRect.Height / 2);
                GraphicsPath barXPGPath = GraphicsExtensions.RoundedRect(ld.rects.Bar(ld.Percent), barRect.Height / 2);
                Region barInnerClipPath = new(GraphicsExtensions.RoundedRect(new Rectangle(barRect.X + 2, barRect.Y + 2, barRect.Width - 4, barRect.Height - 4), barRect.Height / 2 - 2));
                Region levelRenderArea = new(barGPath);
                g.Clip = levelRenderArea;
                g.FillPath(new SolidBrush(System.Drawing.Color.FromArgb(0xe0, System.Drawing.Color.Black)), barGPath);
                g.Clip = barInnerClipPath;
                g.FillPath(xpColor, barXPGPath);
                ColorMatrix colorized = System.Drawing.Color.FromArgb((int)(255 * prefs.LevelOpacity), System.Drawing.Color.White).ToColorMatrix();
                ImageAttributes attr = new();
                attr.SetColorMatrix(colorized);
                using (System.Drawing.Image levelBox = System.Drawing.Image.FromFile(ld.rects.leveltype == LevelRect.LevelBarType.Normal ? barPath : hybridBarPath))
                {
                    levelRenderArea.Complement(new Region(ld.rects.fullRect));
                    g.Clip = levelRenderArea;
                    g.DrawImage(levelBox, ld.rects.fullRect, 0, 0, levelBox.Width, levelBox.Height, GraphicsUnit.Pixel, attr);
                }
                g.Clip = new Region();
                g.DrawString(ld.xpType, fontTitle, whiteColor, ld.rects.typeLabel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                if (ld.rects.rankLabel != default)
                    g.DrawString("RANK", fontMini, whiteColor, ld.rects.rankLabel, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                if (ld.rects.rankText != default)
                    g.DrawString($"#{ld.rank}", fontTitle, whiteColor, ld.rects.rankText, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far });
                g.DrawString(ld.level.ToString(), fontTitle, xpColor, ld.rects.currentLevel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                if (ld.rects.nextLevel != default)
                    g.DrawString((ld.level + 1).ToString(), fontTitle, xpColor, ld.rects.nextLevel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                if (ld.isHybrid)
                {
                    SolidBrush overXPColor = new(xpColor.Color.GetBrightness() < 0.5 ? System.Drawing.Color.White : System.Drawing.Color.Black);
                    g.DrawString(ld.XpExpr, fontDefault, xpColor, ld.rects.expText, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far });
                    g.Clip = new Region(barXPGPath);
                    g.DrawString(ld.XpExpr, fontDefault, overXPColor, ld.rects.expText, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far });
                    g.Clip = new Region();
                }
                else
                {
                    g.DrawString(ld.XpExpr, fontDefault, xpColor, ld.rects.expText, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                }
            }
        }

        /// <summary>
        /// Runs a speed test on the calculation speed of inverse level calculations from XP.
        /// </summary>
        /// <returns></returns>

        [Command("testlevelcalculationspeed")]
        [RequireModerator]

        public async Task RunSpeedTest()
        {
            const int rep = 500000;
            for (int size = 1000; size < 100000000; size *= 10)
            {
                System.Diagnostics.Stopwatch t = new();
                t.Start();

                Random r = new();
                for (int i = 0; i < rep; i++)
                {
                    LevelingConfiguration.GetLevelFromXP(r.Next(0, size), out _, out _);
                }

                t.Stop();
                await Context.Channel.SendMessageAsync($"Calculation of {rep} level calculations completed in {t.ElapsedMilliseconds} milliseconds for size = {size}; level <= {LevelingConfiguration.GetLevelFromXP(size, out _, out _)}.");
            }
        }

        /// <summary>
        /// Checks whether the level calculation method works for any xp value up to a cap.
        /// </summary>
        /// <returns></returns>

        [Command("testlevelcalculationintegrity")]
        [RequireModerator]

        public async Task RunIntegrityTest()
        {
            const int cap = 100000000;
            IUserMessage report = await Context.Channel.SendMessageAsync($"Running Integrity Test: 0/{cap} XP values.");
            int nextAnnounce = 100;
            for (int v = 1; v < cap; v++)
            {
                if (v > nextAnnounce)
                {
                    nextAnnounce *= 5;
                    await report.ModifyAsync(m => m.Content = $"Running Integrity Test: {v}/{cap} XP values.");
                }
                LevelingConfiguration.GetLevelFromXP(v, out _, out _);
            }
            await Context.Channel.SendMessageAsync($"Success up to XP = {cap}!");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
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

            int totalLevel = UserLevel.TotalLevel(LevelingConfiguration, txtlvl, vclvl);

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
        private const int labelIntrusionPixels = 10;
        private const int labelHeight = 60;
        private const int typeLabelWidth = 175;
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
                typeLabel = new Rectangle(defMargin, originHeight, typeLabelWidth, labelHeight);
                rankLabel = new Rectangle(defMargin + typeLabelWidth, originHeight, miniLabelWidth + labelIntrusionPixels, labelHeight);
                rankText = new Rectangle(defMargin + miniLabelWidth + typeLabelWidth, originHeight, levelWidth * 2 / 3 - miniLabelWidth - typeLabelWidth - defMargin, labelHeight);
                expText = new Rectangle(levelWidth / 3, originHeight, levelWidth * 2 / 3, labelHeight);
            }
        }

        private static readonly Dictionary<long, char> units = new() {
            { 1000000000000, 'T'},
            { 1000000000, 'B' },
            { 1000000, 'M' },
            { 1000, 'K'}
        };

        /// <summary>
        /// Converts an XP amount into a shortened version using suffixes.
        /// </summary>
        /// <param name="v">The value to simplify.</param>
        /// <returns>A string containing the shortened value.</returns>

        public static string ToUnit(long v) {
            foreach (KeyValuePair<long, char> kvp in units) {
                if (v >= kvp.Key) {
                    return $"{(float)v /kvp.Key:G3}{kvp.Value}";
                }
            }
            return v.ToString();
        }

        internal class LevelData {
            public int level;
            public long rxp;
            public long lxp;
            public float Percent => rxp / (float)lxp;
            public string XpExpr => $"{ToUnit(rxp)} / {ToUnit(lxp)} XP";
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

            LevelData mainLvlData = new(ul.TextXP > ul.VoiceXP ? ul.TextXP : ul.VoiceXP, mainLevel, LevelingConfiguration);
            LevelData secondaryLvlData = new(ul.TextXP > ul.VoiceXP ? ul.VoiceXP : ul.TextXP, secondaryLevel, LevelingConfiguration);

            int txtrank;
            int vcrank;

            List<UserLevel> allUsers = LevelingDB.Levels.ToList();
            allUsers.Sort((a, b) => b.TextXP.CompareTo(a.TextXP));
            txtrank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;
            allUsers.Sort((a, b) => b.VoiceXP.CompareTo(a.VoiceXP));
            vcrank = allUsers.FindIndex(ul => ul.UserID == user.Id) + 1;

            if (ul.TextXP > ul.VoiceXP) {
                mainLvlData.rank = txtrank;
                mainLvlData.xpType = "Text";
                secondaryLvlData.rank = vcrank;
                secondaryLvlData.xpType = "Voice";
            } else {
                mainLvlData.rank = vcrank;
                mainLvlData.xpType = "Voice";
                secondaryLvlData.rank = txtrank;
                secondaryLvlData.xpType = "Text";
            }

            int totallevel = UserLevel.TotalLevel(LevelingConfiguration, mainLvlData.level, secondaryLvlData.level);

            using (Graphics g = Graphics.FromImage(result)) {
                try {
                    using System.Drawing.Image bg = System.Drawing.Image.FromFile(BackgroundPath(settings.Background ?? "default"));
                    g.DrawImage(bg, mainRect);
                } catch (FileNotFoundException) {
                    if (Regex.IsMatch(settings.Background, @"^(#|0x)?[0-9A-F]{6}$", RegexOptions.IgnoreCase)) {
                        System.Drawing.Color bgc = System.Drawing.Color.FromArgb(
                            unchecked((int)(0xff000000 + int.Parse(settings.Background[^6..], System.Globalization.NumberStyles.HexNumber))));
                        g.Clear(bgc);
                    } else
                        g.Clear(System.Drawing.Color.FromArgb(unchecked((int)settings.XpColor)));
                } catch (FileLoadException) {
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

                g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0xd0, System.Drawing.Color.Black)), titleRect);
                g.DrawString("LEVEL", fontMini, xpColor, rectLevelLabel, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                g.DrawString(totallevel.ToString(), fontTitle, xpColor, rectLevelText);
                SizeF offset = g.MeasureString(totallevel.ToString(), fontTitle);
                const int margin = 5;
                g.DrawString($"({UserLevel.TotalLevelStr(LevelingConfiguration, mainLvlData.level, secondaryLvlData.level)})", fontDefault, xpColor
                    , new Rectangle(rectLevelText.X + (int)offset.Width + margin, rectLevelText.Y, widthmain / 2 - miniLabelWidth - margin - (int)offset.Width, labelHeight)
                    , new StringFormat { LineAlignment = StringAlignment.Far });
                g.DrawString($"{user.Username}#{user.Discriminator}", fontDefault, whiteColor, rectName, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
                
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
                    } else {
                        g.DrawImage(pfp, rectPfp);
                    }
                }

                foreach (LevelData ld in new LevelData[] { mainLvlData, secondaryLvlData }) {
                    g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0xe0, System.Drawing.Color.Black)), ld.rects.Bar(1));
                    g.FillRectangle(xpColor, ld.rects.Bar(ld.Percent));
                    using (System.Drawing.Image levelBox = System.Drawing.Image.FromFile(barPath)) {
                        g.DrawImage(levelBox, ld.rects.fullRect);
                    }
                    g.DrawString(ld.xpType, fontTitle, whiteColor, ld.rects.typeLabel);
                    g.DrawString("RANK", fontMini, whiteColor, ld.rects.rankLabel, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                    g.DrawString($"#{ld.rank}", fontTitle, whiteColor, ld.rects.rankText);
                    g.DrawString(ld.level.ToString(), fontTitle, xpColor, ld.rects.currentLevel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    g.DrawString((ld.level + 1).ToString(), fontTitle, xpColor, ld.rects.nextLevel, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    g.DrawString(ld.XpExpr, fontDefault, xpColor, ld.rects.expText, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far });
                }
            }

            return result;
        }
    }
}

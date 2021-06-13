using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("color")]
        
        public async Task SupporterRolesCommand(string colorname) {

            if (colorname == "list") {
                await PrintColorOptions();
                return;
            }

            IEnumerable<IRole> roles = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Roles;

            IRole toAdd = null;
            foreach(IRole role in roles) {
                if (role.Name.ToLower().StartsWith(UtilityConfiguration.ColorRolePrefix.ToLower() + colorname.ToLower())) {
                    toAdd = role;
                    break;
                }
            }

            if (toAdd is null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Color not found")
                    .WithDescription($"Unable to find the specified color \"{colorname}\". To see a list of colors and their names, use the `color list` command.")
                    .SendEmbed(Context.Channel);
                return;
            }
        }

        private async Task PrintColorOptions() {

            string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
            string filepath = Path.Join(imageChacheDir, $"ColorsList.jpg");

            if (!File.Exists(filepath)) {
                await ReloadColorList();
            }

            await Context.Channel.SendFileAsync(filepath);
        }

        [Command("reloadcolors")]
        [Summary("Updates the color list graphic to reflect new settings and new added roles")]
        [RequireModerator]

        public async Task ReloadColorListCommand() {
            await ReloadColorList(true);
        }

        const int IconRoleNameMargin = 10;

        private async Task ReloadColorList(bool verbose = false) {
            IEnumerable<IRole> roles = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Roles;
            List<IRole> colorRoles = new();

            foreach (IRole role in roles) {
                if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix)) {
                    colorRoles.Add(role);
                }
            }

            if (verbose && !colorRoles.Any()) {
                await Context.Channel.SendMessageAsync($"No roles found with the prefix: \"{UtilityConfiguration.ColorRolePrefix}\".");
                return;
            }

            int colwidth = UtilityConfiguration.ColorListColWidth;
            int colcount = UtilityConfiguration.ColorListColCount;
            int rowheight = UtilityConfiguration.ColorListRowHeight;
            int rowcount = (colorRoles.Count - 1) / colcount + 1;
            int height =  rowcount * rowheight;

            string fontPath = Path.Join(Directory.GetCurrentDirectory(), "Images", "OtherMedia", "Fonts", "Whitney", "whitneymedium.otf");

            PrivateFontCollection privateFontCollection = new();
            privateFontCollection.AddFontFile(fontPath);
            FontFamily fontfamily = privateFontCollection.Families.First();
            Font font = new(fontfamily, UtilityConfiguration.ColorListFontSize, FontStyle.Regular, GraphicsUnit.Pixel);

            Bitmap picture = new Bitmap(colwidth * colcount, height);
            using (Graphics g = Graphics.FromImage(picture)) {
                int col = 0;
                int row = 0;

                using System.Drawing.Image icon = System.Drawing.Image.FromFile(Path.Join(Directory.GetCurrentDirectory(), "Images", "PawIcon.png"));
                foreach (IRole role in colorRoles) {
                    using Brush brush = new SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)(role.Color.RawValue + 0xFF000000))));

                    ColorMatrix colorMatrix = BasicTransform(role.Color.R / 255f, role.Color.G / 255f, role.Color.B / 255f);
                    ImageAttributes imageAttributes = new();
                    imageAttributes.SetColorMatrix(colorMatrix);
                    Console.Out.WriteLine($"Role {role.Name}: ({row}, {col})");
                    g.DrawImage(icon, 
                        new Rectangle(col * colwidth, row * rowheight, rowheight, rowheight), 
                        0, 0, icon.Width, icon.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes);
                    g.DrawString(ToRoleName(role), font, brush,
                        col * colwidth + rowheight + IconRoleNameMargin, (rowheight - UtilityConfiguration.ColorListFontSize) / 2);

                    if (UtilityConfiguration.ColorListDisplayByRows) {
                        row++;
                        if (row >= rowcount) {
                            row = 0;
                            col++;
                        }
                    }
                    else {
                        col++;
                        if (col >= colcount) {
                            col = 0;
                            row++;
                        }
                    }
                }
            }

            string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
            string filepath = Path.Join(imageChacheDir, $"ColorsList.jpg");
            picture.Save(filepath);

            if (verbose) await Context.Channel.SendMessageAsync($"Successfully reloaded colors!");
        }

        private readonly float[] alphatransform = new float[] { 0, 0, 0, 1, 0 };
        private readonly float[] lineartransform = new float[] { 0, 0, 0, 0, 1 };

        private ColorMatrix BasicTransform(float r, float g, float b) {
            return new ColorMatrix(new float[][] {
                new float[] {r, 0, 0, 0, 0},
                new float[] {0, g, 0, 0, 0},
                new float[] {0, 0, b, 0, 0},
                alphatransform,
                lineartransform
                });
        }

        private string ToRoleName(IRole role) {
            return role.Name.Substring(UtilityConfiguration.ColorRolePrefix.Length);
        }
    }

}

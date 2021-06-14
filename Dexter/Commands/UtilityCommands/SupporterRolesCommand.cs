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
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        /// <summary>
        /// Sets the role color of users with the permission or displays the list of available color roles.
        /// </summary>
        /// <param name="colorname"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("color")]
        [Summary("Changes your color role or lists all available color roles.")]
        [ExtendedSummary("Changes your color role or lists all available color roles.\n" +
            "`color LIST` - displays a list of available color roles\n" +
            "`color NONE` - removes all color roles from you\n" +
            "`color [colorname]` - changes your current color role to the one specified.")]
        
        public async Task SupporterRolesCommand([Remainder] string colorname) {

            if (colorname == "list") {
                await PrintColorOptions();
                return;
            }

            IGuildUser user;
            if (Context.User is not IGuildUser) {
                user = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(Context.User.Id);
                if (user is null) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to find user!")
                        .WithDescription("This could be due to caching errors, but I couldn't find you in the server! Try again later, if this persists, contact the developer team or an administrator.")
                        .SendEmbed(Context.Channel);
                    return;
                }
            } else {
                user = (IGuildUser) Context.User;
            }

            IEnumerable<IRole> roles = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Roles;
            Dictionary<ulong, IRole> colorRoleIDs = new();

            IRole toAdd = null;
            bool found = false;
            foreach (IRole role in roles) {
                if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix)) {
                    colorRoleIDs.Add(role.Id, role);
                    if (!found && role.Name.ToLower().EndsWith(colorname.ToLower())) {
                        toAdd = role;
                        found = true;
                    }
                }
            }
            bool canChangeColor = CanChangeColor(user);
            if (!canChangeColor || colorname.ToLower() == "none") {
                if (!await TryRemoveRoles(user, colorRoleIDs)) return;

                if (!canChangeColor)
                    await Context.Channel.SendMessageAsync("You don't have the necessary roles to change your color role!");
                else
                    await Context.Channel.SendMessageAsync("Removed color roles!");
                return;
            }

            if (UtilityConfiguration.LockedColors.ContainsKey(toAdd.Id) 
                && !user.RoleIds.Contains(UtilityConfiguration.LockedColors[toAdd.Id])) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Locked Role!")
                    .WithDescription($"In order to unlock this role, you must first get <@&{UtilityConfiguration.LockedColors[toAdd.Id]}>.")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (toAdd is null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Color not found")
                    .WithDescription($"Unable to find the specified color \"{colorname}\". To see a list of colors and their names, use the `color list` command.")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (!await TryRemoveRoles(user, colorRoleIDs)) return;
            try {
                await user.AddRoleAsync(toAdd);
            }
            catch {
                await Context.Channel.SendMessageAsync("Unable to add role! Missing required permissions.");
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Successfully added new role!")
                .WithDescription($"Changed your color role to: {toAdd.Name}.")
                .SendEmbed(Context.Channel);
        }

        private async Task<bool> TryRemoveRoles(IGuildUser user, Dictionary<ulong, IRole> colorRoleIDs) {
            List<IRole> toRemove = new();
            foreach(ulong roleID in user.RoleIds) {
                if (colorRoleIDs.ContainsKey(roleID)) {
                    toRemove.Add(colorRoleIDs[roleID]);
                }
            }

            if (!toRemove.Any()) return true;

            try {
                await user.RemoveRolesAsync(toRemove);
                return true;
            }
            catch(HttpException e) {
                await Context.Channel.SendMessageAsync($"Missing permissions for role management! {e}");
                return false;
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

        /// <summary>
        /// Reloads the emote list image from the given configuration.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("reloadcolors")]
        [Summary("Updates the color list graphic to reflect new settings and new added roles")]
        [RequireModerator]

        public async Task ReloadColorListCommand() {
            await ReloadColorList(true);
        }

        const int IconRoleNameMargin = 10;

        private async Task ReloadColorList(bool verbose = false) {
            List<SocketRole> roles = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Roles.ToList();
            roles.Sort((ra, rb) => rb.Position.CompareTo(ra.Position));
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

            Bitmap picture = new(colwidth * colcount, height);
            using (Graphics g = Graphics.FromImage(picture)) {
                int col = 0;
                int row = 0;

                using System.Drawing.Image icon = System.Drawing.Image.FromFile(Path.Join(Directory.GetCurrentDirectory(), "Images", "PawIcon.png"));
                foreach (IRole role in colorRoles) {
                    using Brush brush = new SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)(role.Color.RawValue + 0xFF000000))));

                    ColorMatrix colorMatrix = BasicTransform(role.Color.R / 255f, role.Color.G / 255f, role.Color.B / 255f);
                    ImageAttributes imageAttributes = new();
                    imageAttributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(icon, 
                        new Rectangle(col * colwidth, row * rowheight, rowheight, rowheight), 
                        0, 0, icon.Width, icon.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes);
                    g.DrawString(ToRoleName(role), font, brush,
                        col * colwidth + rowheight + IconRoleNameMargin, (rowheight - UtilityConfiguration.ColorListFontSize) / 2 + row * rowheight);

                    if (UtilityConfiguration.ColorListDisplayByRows) {
                        col++;
                        if (col >= colcount) {
                            col = 0;
                            row++;
                        }
                    }
                    else {
                        row++;
                        if (row >= rowcount) {
                            row = 0;
                            col++;
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
            return role.Name[UtilityConfiguration.ColorRolePrefix.Length..];
        }

        private bool CanChangeColor(IGuildUser user) {
            if (user is null) return false;

            foreach(ulong roleId in user.RoleIds) {
                if (UtilityConfiguration.ColorChangeRoles.Contains(roleId)) return true;
            }
            return false;
        }
    }

}

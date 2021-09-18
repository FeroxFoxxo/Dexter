using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Displays all configured ranked roles and relevant information about them in an image sent to the contextual channel.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("rankedroles")]
        [Alias("rankroles", "levelroles")]
        [Summary("Displays all configured ranked roles and what level they are obtained at.")]
        [BotChannel]

        public async Task DisplayRankedRolesCommand()
        {
            await RenderRankedRoles(GetRankedRoles())
                .Send(Context.Channel);
        }

        private SortedDictionary<int, IRole> GetRankedRoles()
        {
            SortedDictionary<int, IRole> result = new();
            IGuild guild = DiscordSocketClient.GetGuild(BotConfiguration.GuildID);

            foreach (KeyValuePair<int, ulong> roleEntry in LevelingConfiguration.Levels)
            {
                result.Add(roleEntry.Key, guild.GetRole(roleEntry.Value));
            }

            return result;
        }

        const int rowheight = 30;
        const int colwidth = 350;
        const int sidepadding = 10;

        private readonly string iconPath = Path.Join(Directory.GetCurrentDirectory(), "Images", "PawIcon.png");

        private System.Drawing.Image RenderRankedRoles(SortedDictionary<int, IRole> roles)
        {
            Bitmap result = new(colwidth, rowheight * roles.Count);

            using (Graphics g = Graphics.FromImage(result))
            {

                using System.Drawing.Image icon = System.Drawing.Image.FromFile(iconPath);
                int row = 0;
                foreach (KeyValuePair<int, IRole> role in roles)
                {
                    ColorMatrix colorMatrix = UtilityCommands.BasicTransform(role.Value.Color.R / 255f, role.Value.Color.G / 255f, role.Value.Color.B / 255f);
                    ImageAttributes imageAttributes = new();
                    imageAttributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(icon,
                        new Rectangle(0, row * rowheight, rowheight, rowheight),
                        0, 0, icon.Width, icon.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes);

                    SolidBrush colorBrush = new(role.Value.ToGraphicsColor());
                    Rectangle textArea = new(rowheight + sidepadding, row * rowheight, colwidth - rowheight - 2 * sidepadding, rowheight);
                    string fontPath = Path.Join(Directory.GetCurrentDirectory(), "Images", "OtherMedia", "Fonts", "Whitney", "whitneymedium.otf");

                    PrivateFontCollection privateFontCollection = new();
                    privateFontCollection.AddFontFile(fontPath);
                    FontFamily fontfamily = privateFontCollection.Families.First();
                    Font font = new(fontfamily, rowheight * 2 / 3, FontStyle.Regular, GraphicsUnit.Pixel);

                    g.DrawString(role.Value.Name, font, colorBrush, textArea, new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                    g.DrawString($"Level {role.Key}", font, colorBrush, textArea, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
                    row++;
                }
            }

            return result;
        }

    }
}

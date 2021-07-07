using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Extensions {

    /// <summary>
    /// Contains useful extensions and shorthands for graphics-related methods
    /// </summary>

    public static class GraphicsExtensions {

        /// <summary>
        /// Converts a discord role's color into a graphics-readable value (with no transparency)
        /// </summary>
        /// <param name="role">The target role to extract the base color from</param>
        /// <returns>A <see cref="System.Drawing.Color"/> object that can be used in brushes and the like.</returns>

        public static System.Drawing.Color ToGraphicsColor(this Discord.IRole role) {
            return role.Color.ToGraphicsColor();
        }

        /// <summary>
        /// Converts a discord color into a graphics-readable value (with no transparency)
        /// </summary>
        /// <param name="color">The target color to convert to a Graphics color.</param>
        /// <returns>A <see cref="System.Drawing.Color"/> object that can be used in brushes and the like.</returns>

        public static System.Drawing.Color ToGraphicsColor(this Discord.Color color) {
            return System.Drawing.Color.FromArgb(unchecked((int)(color.RawValue + 0xFF000000)));
        }

        private static readonly string tempCachePath = Path.Join(Directory.GetCurrentDirectory(), "ImageCache", "tempImage.png");

        /// <summary>
        /// Sends an image into a given message channel.
        /// </summary>
        /// <param name="image">The image to send</param>
        /// <param name="channel">The channel to send the image to</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public static async Task Send(this System.Drawing.Image image, Discord.IMessageChannel channel) {
            image.Save(tempCachePath);
            await channel.SendFileAsync(tempCachePath);
            File.Delete(tempCachePath);
        }

    }
}

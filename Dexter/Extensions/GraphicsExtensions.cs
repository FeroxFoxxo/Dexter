using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Dexter.Extensions
{

	/// <summary>
	/// Contains useful extensions and shorthands for graphics-related methods
	/// </summary>

	public static class GraphicsExtensions
	{

		/// <summary>
		/// Converts a discord role's color into a graphics-readable value (with no transparency)
		/// </summary>
		/// <param name="role">The target role to extract the base color from</param>
		/// <returns>A <see cref="Color"/> object that can be used in brushes and the like.</returns>

		public static Color ToGraphicsColor(this Discord.IRole role)
		{
			return role.Color.ToGraphicsColor();
		}

		/// <summary>
		/// Converts a discord color into a graphics-readable value (with no transparency)
		/// </summary>
		/// <param name="color">The target color to convert to a Graphics color.</param>
		/// <returns>A <see cref="Color"/> object that can be used in brushes and the like.</returns>

		public static Color ToGraphicsColor(this Discord.Color color)
		{
			return Color.FromArgb(unchecked((int)(color.RawValue + 0xFF000000)));
		}

		private static readonly string tempCachePath = Path.Join(Directory.GetCurrentDirectory(), "ImageCache", "tempImage.png");

		/// <summary>
		/// Sends an image into a given message channel.
		/// </summary>
		/// <param name="image">The image to send</param>
		/// <param name="channel">The channel to send the image to</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public static async Task Send(this Image image, Discord.IMessageChannel channel)
		{
			image.Save(tempCachePath);
			await channel.SendFileAsync(tempCachePath);
			File.Delete(tempCachePath);
		}

		private readonly static float[] lineartransform = new float[] { 0, 0, 0, 0, 1 };

		/// <summary>
		/// Creates a color matrix from a given color.
		/// </summary>
		/// <param name="color">The base color to create a transformation off of.</param>
		/// <returns>A color matrix that can be used to recolorize images.</returns>

		public static System.Drawing.Imaging.ColorMatrix ToColorMatrix(this Color color)
		{
			return new System.Drawing.Imaging.ColorMatrix(new float[][] {
				new float[] {color.R / 255f, 0, 0, 0, 0},
				new float[] {0, color.G / 255f, 0, 0, 0},
				new float[] {0, 0, color.B / 255f, 0, 0},
				new float[] {0, 0, 0, color.A / 255f, 0},
				lineartransform
				});
		}

		/// <summary>
		/// Creates a rounded rectangle path with a given set of bounds and a radius.
		/// </summary>
		/// <param name="bounds">The outer rectangle to inscribe the resulting rounded rectangle in.</param>
		/// <param name="radius">The rounding radius for the rectangle.</param>
		/// <returns>A <see cref="GraphicsPath"/> that describes a rounded rectangle with the given parameters.</returns>

		public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
		{
			int diameter = radius * 2;
			Size size = new(diameter, diameter);
			Rectangle arc = new(bounds.Location, size);
			GraphicsPath path = new();

			if (radius == 0)
			{
				path.AddRectangle(bounds);
				return path;
			}

			// top left arc  
			path.AddArc(arc, 180, 90);

			// top right arc  
			arc.X = bounds.Right - diameter;
			path.AddArc(arc, 270, 90);

			// bottom right arc  
			arc.Y = bounds.Bottom - diameter;
			path.AddArc(arc, 0, 90);

			// bottom left arc 
			arc.X = bounds.Left;
			path.AddArc(arc, 90, 90);

			path.CloseFigure();
			return path;
		}

	}
}

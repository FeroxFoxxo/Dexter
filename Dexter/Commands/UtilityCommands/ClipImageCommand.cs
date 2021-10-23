using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace Dexter.Commands
{
	public partial class UtilityCommands
	{

		/// <summary>
		/// Crops a given image into a given target size with a set of options.
		/// </summary>
		/// <param name="targetSize">The target size expressed as text; as resolution or as a ratio.</param>
		/// <param name="options">Any relevant options</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("clipimage")]
		[Alias("crop", "cropimg", "cropimage", "clipimg", "clip")]
		[Summary("Usage: `clipimage [dimensions] (options...)`")]
		[ExtendedSummary("Usage: `clipimage [dimensions] (options...)` where `dimensions` is either a resolution in the format `NxN` or a ratio in the format `N:N`, where \"N\" represents a whole number.\n" +
			"Valid options are:\n" +
			"`circle` - Crops the image into a circle (or ellipse).\n" +
			"`shiftX` or `shiftX%` where \"X\" is a decimal between 0 and 1 and \"X%\" is a decimal number between 0 and 100 followed by a percent sign. - shifts the crop location from the topmost or leftmost side to the image a percentage of the image height or width to the bottom or right.\n" +
			"`unscaled` - Makes it so, if the source cropped image would be bigger than the resolution you specified, the crop is instead reduced to a smaller fraction of the image to avoid scaling.")]
		[BotChannel]

		public async Task ClipImageCommand(string targetSize, [Remainder] string options = "")
		{

			Attachment att = Context.Message.Attachments.FirstOrDefault();
			string format = ".png";

			if (att is null || !(att.Filename.EndsWith(".jpg") || att.Filename.EndsWith(".png")))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("No valid attachment found")
					.WithDescription("You must send a png or jpg image with the command in order to treat it.")
					.SendEmbed(Context.Channel);
				return;
			}

			if (att.Filename.EndsWith(".jpg")) format = ".jpg";

			int targetWidth = -1;
			float targetRatio = 1;
			switch (targetSize.ToLower())
			{
				case "rankcard":
					targetWidth = LevelingCommands.RankCardSize.Width;
					targetRatio = LevelingCommands.RankCardSize.Width / (float)LevelingCommands.RankCardSize.Height;
					break;
				default:
					string dimsStr = Regex.Match(targetSize, @"[0-9]{1,11}x[0-9]{1,11}").Value;
					string ratioStr = Regex.Match(targetSize, @"[0-9]{1,11}:[0-9]{1,11}").Value;

					string[] numbersStr;
					int num1;
					int num2;
					if (!string.IsNullOrEmpty(dimsStr))
					{
						numbersStr = dimsStr.Split('x');
						num1 = int.Parse(numbersStr[0]);
						num2 = int.Parse(numbersStr[1]);

						targetWidth = num1;
						targetRatio = num1 / (float)num2;
					}
					else if (!string.IsNullOrEmpty(ratioStr))
					{
						numbersStr = ratioStr.Split(':');
						num1 = int.Parse(numbersStr[0]);
						num2 = int.Parse(numbersStr[1]);

						targetRatio = num1 / (float)num2;
					}
					else
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Invalid rank card size expression")
							.WithDescription("Please use an expression of the form: `NxN` or `N:N` where `N` is a whole number below 100000000000 and without thousands divisions (,).")
							.SendEmbed(Context.Channel);
						return;
					}
					break;
			}

			bool toCircle = false;
			bool scaled = true;
			float shift = 0;
			List<string> unrecognizedSymbols = new();
			foreach (string opt in options.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				switch (opt.ToLower())
				{
					case "circle":
						toCircle = true;
						break;
					case "unscaled":
						scaled = false;
						break;
					default:
						string shiftStr = Regex.Match(opt, @"shift[0-9]+(.[0-9]+)?%?", RegexOptions.IgnoreCase).Value;
						if (!string.IsNullOrEmpty(shiftStr))
						{
							bool isPercent = shiftStr.EndsWith('%');
							if (isPercent)
								shiftStr = shiftStr[0..^1];
							shift = float.Parse(shiftStr["shift".Length..]) / (isPercent ? 100 : 1);
							if (shift > 1) shift = 1;
						}
						else
						{
							unrecognizedSymbols.Add(opt);
						}
						break;
				}
			}

			HttpClient web = new();
			byte[] data = await web.GetByteArrayAsync(att.Url);
			using MemoryStream mem = new(data);
			using System.Drawing.Image img = System.Drawing.Image.FromStream(mem);

			web.Dispose();
			mem.Dispose();

			int actualWidth = targetWidth <= 0 ? img.Width : targetWidth;

			int fromWidth = img.Width;
			int fromHeight = (int)(fromWidth / targetRatio);

			if (fromHeight > img.Height)
			{
				fromHeight = img.Height;
				fromWidth = (int)(fromHeight * targetRatio);
			}

			if (!scaled)
			{
				if (fromHeight > targetWidth * targetRatio && fromWidth > targetWidth)
				{
					fromHeight = (int)(targetWidth * targetRatio);
					fromWidth = targetWidth;
				}
			}

			int fromX = (int)((img.Width - fromWidth) * shift);
			int fromY = (int)((img.Height - fromHeight) * shift);

			using Bitmap result = new(actualWidth, (int)(actualWidth / targetRatio));
			using (Graphics g = Graphics.FromImage(result))
			{
				Rectangle fullrect = new(Point.Empty, result.Size);

				if (toCircle)
				{
					using GraphicsPath clipPath = new();
					clipPath.AddEllipse(fullrect);
					g.Clip = new Region(clipPath);
				}

				g.DrawImage(img, fullrect,
					fromX, fromY, fromWidth, fromHeight, GraphicsUnit.Pixel);
			}

			string path = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache", "temp_clipped" + format);
			result.Save(path);

			await Context.Channel.SendFileAsync(path);

			File.Delete(path);
		}

	}
}

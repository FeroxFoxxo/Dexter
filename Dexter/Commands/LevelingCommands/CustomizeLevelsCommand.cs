using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Text.RegularExpressions;

namespace Dexter.Commands
{
	public partial class LevelingCommands
	{

		/// <summary>
		/// Handles the modification of profile-specific level system prefrences.
		/// </summary>
		/// <param name="attribute">The attribute to modify.</param>
		/// <param name="value">The value to set the attribute to.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("rankcard")]
		[Alias("configlevel")]
		[Summary("Usage: `rankcard [Attribute] (Value)` - Used to modify the appearance of your rank card.")]
		[ExtendedSummary("Usage: `rankcard [Attribute] (Value)`\n" +
			"You may see all attributes and their encoded names by using the `rankcard` command without any parameters. In order to inquire more about a specific attribute, use the `rankcard [Attribute]` command without specifying a value.\n" +
			"Examples:\n" +
			"`rankcard` - get general attribute infomation and your personal settings\n" +
			"`rankcard color #ff0000` - set your xpcolor to red\n" +
			"`rankcard pfpborder` - inquire about the possible effects and values of the pfpborder attribute.")]
		[BotChannel]

		public async Task CustomizeLevelsCommand(string attribute = "", [Remainder] string value = "")
		{
			UserLevel ul;
			LevelPreferences prefs;
			try
			{
				ul = LevelingDB.GetOrCreateLevelData(Context.User.Id, out prefs);
			}
			catch (System.Exception e)
			{
				await Context.Channel.SendMessageAsync($"Whoops! We had an issue trying to access your data in the database. Try again in a few seconds...\n" +
					$"If this error persists, please ping the Development Committee so a solution can be reached!\n" +
					$"Exception Trace: {e}");
				return;
			}

			switch (attribute.ToLower())
			{
				case "color":
				case "colour":
				case "xpcolor":
				case "xpcolour":
					if (string.IsNullOrEmpty(value))
					{
						await BuildEmbed(EmojiEnum.Sign)
							.WithTitle("Color Information")
							.WithDescription("You can set a custom color for your XP display. Use either the name of an existing color such as \"Red\" or a hexadecimal string such as \"#df21fe\".")
							.SendEmbed(Context.Channel);
						return;
					}
					System.Drawing.Color color;
					color = System.Drawing.Color.FromName(value);
					if (color.A == 0)
					{
						if (!Regex.IsMatch(value, @"^(0x|#)?[0-9A-F]{6}$", RegexOptions.IgnoreCase))
						{
							await BuildEmbed(EmojiEnum.Annoyed)
								.WithTitle("Unable to parse color!")
								.WithDescription($"The color {value} isn't a known color name nor follows a hexadecimal format with 6 digits.")
								.SendEmbed(Context.Channel);
							return;
						}
						int hex = int.Parse(value[^6..], System.Globalization.NumberStyles.HexNumber);
						color = System.Drawing.Color.FromArgb(unchecked((int)(hex + 0xff000000)));
					}
					prefs.XpColor = (ulong)color.ToArgb();
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Success!")
						.WithDescription($"Your rank card will now be rendered using the color: {color.Name}")
						.SendEmbed(Context.Channel);
					break;
				case "pfpborder":
				case "pfpbackground":
					switch (value.ToLower())
					{
						case "true":
						case "yes":
						case "enabled":
							prefs.PfpBorder = true;
							break;
						case "false":
						case "no":
						case "disabled":
							prefs.PfpBorder = false;
							break;
						default:
							await BuildEmbed(EmojiEnum.Sign)
								.WithTitle("Information about \"pfpborder\"")
								.WithDescription("Describes whether a gray circle should be rendered around your profile picture in your rank card.")
								.AddField("Possible Values", "**true**: A circle will be rendered.\n**false**: A circle will not be rendered.")
								.SendEmbed(Context.Channel);
							return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription($"The value of \"pfpborder\" has been set to `{prefs.PfpBorder}`.")
						.SendEmbed(Context.Channel);
					break;
				case "croppfp":
				case "pfpcrop":
					switch (value.ToLower())
					{
						case "true":
						case "yes":
						case "enabled":
						case "circle":
							prefs.CropPfp = true;
							break;
						case "false":
						case "no":
						case "disabled":
						case "square":
							prefs.CropPfp = false;
							break;
						default:
							await BuildEmbed(EmojiEnum.Sign)
								.WithTitle("Information about \"croppfp\"")
								.WithDescription("Describes whether your profile picture should be cropped into a circle or remain a full square in your rank card.")
								.AddField("Possible Values", "**true**: The picture will be cropped.\n**false**: The picture will be rendered in full.")
								.SendEmbed(Context.Channel);
							return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription($"The value of \"croppfp\" has been set to `{prefs.CropPfp}`.")
						.SendEmbed(Context.Channel);
					break;
				case "namebackground":
				case "namebg":
				case "toplabelbackground":
				case "toplabelbg":
				case "titlebackground":
				case "titlebg":
					switch (value.ToLower())
					{
						case "true":
						case "yes":
						case "enabled":
							prefs.TitleBackground = true;
							break;
						case "false":
						case "no":
						case "disabled":
							prefs.TitleBackground = false;
							break;
						default:
							await BuildEmbed(EmojiEnum.Sign)
								.WithTitle("Information about \"TitleBackground\"")
								.WithDescription("Describes whether the black bar that makes your name and level generally visible should be rendered.")
								.AddField("Possible Values", "**true**: The bar will be rendered.\n**false**: The bar won't be rendered.")
								.SendEmbed(Context.Channel);
							return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription($"The value of \"TitleBackground\" has been set to `{prefs.TitleBackground}`.")
						.SendEmbed(Context.Channel);
					break;
				case "showhybrid":
				case "displayhybrid":
				case "showhybridlevels":
				case "displayhybridlevels":
					switch (value.ToLower())
					{
						case "true":
						case "yes":
						case "enabled":
							prefs.ShowHybrid = true;
							break;
						case "false":
						case "no":
						case "disabled":
							prefs.ShowHybrid = false;
							break;
						default:
							await BuildEmbed(EmojiEnum.Sign)
								.WithTitle("Information about \"ShowHybridLevels\"")
								.WithDescription("Describes whether the individual level types should be displayed if Dexter XP is set to XP Merge Mode.")
								.AddField("Possible Values", "**true**: Individual levels will be displayed.\n**false**: Only the total level will be displayed.")
								.SendEmbed(Context.Channel);
							return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription($"The value of \"ShowHybridLevels\" has been set to `{prefs.ShowHybrid}`.")
						.SendEmbed(Context.Channel);
					break;
				case "insetmain":
				case "insetmainxp":
				case "insetmainexp":
					switch(value.ToLower())
					{
						case "true":
						case "yes":
						case "enabled":
						case "inside":
							prefs.InsetMainXP = true;
							break;
						case "false":
						case "no":
						case "disabled":
						case "outside":
							prefs.InsetMainXP = false;
							break;
						default:
							await BuildEmbed(EmojiEnum.Sign)
								.WithTitle("Information about \"InsetMainXP\"")
								.WithDescription("Displays the user's XP in text form inside the main XP bar, similarly to hybrid levels.")
								.AddField("Possible Values", "**true** or **inside**: XP text will be displayed inside the XP bar.\n**false** or **outside**: XP text will be displayed on the top right corner of the main XP rectangle.")
								.SendEmbed(Context.Channel);
							return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription($"The value of \"InsetMainXP\" has been set to `{prefs.InsetMainXP}`.")
						.SendEmbed(Context.Channel);
					break;
				case "levelalpha":
				case "levelopacity":
					bool isPercent = false;
					if (value.EndsWith('%'))
					{
						isPercent = true;
						value = value[..^1];
					}
					float opacity = 0;
					int discreteOpacity = 0;
					if (!(int.TryParse(value, out discreteOpacity) && discreteOpacity > 1) && !float.TryParse(value, out opacity))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Unable to Parse Value.")
							.WithDescription($"The value \"{value}\" could not be parsed into a valid number.")
							.SendEmbed(Context.Channel);
						return;
					}
					if (discreteOpacity > 1)
					{
						if (isPercent)
							opacity = discreteOpacity / 100f;
						else
							opacity = discreteOpacity / 255f;
					}
					else if (isPercent) opacity /= 100;
					if (opacity > 1 || opacity < 0)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Invalid Opacity Value")
							.WithDescription("Enter a decimal value between 0 and 1 or an integer value between 2 and 255.")
							.SendEmbed(Context.Channel);
						return;
					}
					prefs.LevelOpacity = opacity;
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription($"Your rankcard level template opacity has been set to {opacity * 100:G3}%.")
						.SendEmbed(Context.Channel);
					break;
				case "background":
				case "image":
				case "bgimage":
				case "bgimg":
				case "bg":
					string path = Path.Combine(Directory.GetCurrentDirectory(), "Images", "Levels", "Backgrounds");
					string[] processedPaths = Directory.GetFiles(path);

					for (int i = 0; i < processedPaths.Length; i++)
						processedPaths[i] = processedPaths[i].Split('\\').Last().Split('/').Last()[..^4];
					if (value.ToLower() == "list")
					{
						await BuildEmbed(EmojiEnum.Sign)
							.WithTitle("Default Background Images")
							.WithDescription(string.Join(", ", processedPaths))
							.SendEmbed(Context.Channel);
						return;
					}
					if (!string.IsNullOrEmpty(value))
					{
						if (value.EndsWith(".jpg")) value = value[..^4];
						if (value.EndsWith(".png")) value = value[..^4];

						if (!File.Exists(Path.Combine(path, $"{value.ToLower()}.jpg"))
							&& !File.Exists(Path.Combine(path, $"{value.ToLower()}.png")))
						{
							if (Regex.IsMatch(value, @"^(0x|#)?[0-9A-F]{6}$", RegexOptions.IgnoreCase))
							{
								int hex = int.Parse(value[^6..], System.Globalization.NumberStyles.HexNumber);
								color = System.Drawing.Color.FromArgb(unchecked((int)(hex + 0xff000000)));

								prefs.Background = value;

								await BuildEmbed(EmojiEnum.Love)
									.WithTitle("Background set to flat color!")
									.WithDescription($"Your rank card background will appear as the following solid color: {color.Name}")
									.SendEmbed(Context.Channel);
								break;
							}

							await BuildEmbed(EmojiEnum.Annoyed)
								.WithTitle("Unable to find default image")
								.WithDescription($"The specified value: \"{value}\", doesn't correspond to any of the images available by default:\n" +
								$"{string.Join(", ", processedPaths)}\n" +
								$"Nor does it fit a color format (#FFFFFF) or a link to an image (http...)")
								.SendEmbed(Context.Channel);
							return;
						}
						prefs.Background = value.ToLower();
						await BuildEmbed(EmojiEnum.Love)
							.WithTitle("Background Set!")
							.WithDescription($"Your background has been set to the default image {value}.")
							.SendEmbed(Context.Channel);
						break;
					}
					if (ul.TotalLevel(LevelingConfiguration) < LevelingConfiguration.CustomImageMinimumLevel)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Locked Feature!")
							.WithDescription($"You must reach level {LevelingConfiguration.CustomImageMinimumLevel} in order to use this feature.")
							.SendEmbed(Context.Channel);
						return;
					}
					if (Context.Message.Attachments.FirstOrDefault() is null)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("No Attachments Found")
							.WithDescription($"Send an image in a valid format along with your command! It must be a single message.")
							.SendEmbed(Context.Channel);
						return;
					}
					string url = Context.Message.Attachments.First().ProxyUrl;
					if (!(url.EndsWith(".png") || url.EndsWith(".jpg")))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Invalid Image Format")
							.WithDescription($"Your custom image must either have the extension `.jpg` or `.png`.")
							.SendEmbed(Context.Channel);
						return;
					}
					if (Context.Message.Attachments.FirstOrDefault().Size > LevelingConfiguration.CustomImageSizeLimit)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Maximum File Size Exceeded")
							.WithDescription($"Keep your custom image files below a size of {LevelingConfiguration.CustomImageSizeLimit} bytes.")
							.SendEmbed(Context.Channel);
						return;
					}
					await (DiscordShardedClient.GetGuild(LevelingConfiguration.CustomImageDumpsGuild)
						.GetChannel(LevelingConfiguration.CustomImageDumpsChannel) as ITextChannel)
						.SendMessageAsync(url);
					prefs.Background = url;
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Operation Successful!")
						.WithDescription("Your rank card background has been set to the new image! The resolution will be adjusted accordingly.")
						.SendEmbed(Context.Channel);
					break;
				default:
					await BuildEmbed(EmojiEnum.Sign)
						.WithTitle("Rankcard Display Settings")
						.WithDescription($"Here are the list of preferences you've set for your rank card display:\n" +
						$"XPColor <__*color*__>: #{prefs.XpColor & 0x00ffffff:X}\n" +
						$"Background Image <__*bg*__>: {(prefs.Background.StartsWith("http") ? $"[View]({prefs.Background})" : prefs.Background)}\n" +
						$"Pfp border <__*pfpborder*__>: **{prefs.PfpBorder}**\n" +
						$"Crop Pfp <__*croppfp*__>: **{prefs.CropPfp}**\n" +
						$"Title Background <__*titlebg*__>: **{prefs.TitleBackground}**\n" +
						$"Show Hybrid Levels <__*showhybrid*__>: **{prefs.ShowHybrid}** {(LevelingConfiguration.LevelMergeMode is Configurations.LevelMergeMode.AddXPMerged or Configurations.LevelMergeMode.AddXPSimple ? "" : " *(disabled due to Dexter XP Merge Mode.)*")}\n" +
						$"Level Opacity <__*levelopacity*__>: **{prefs.LevelOpacity * 100:G3}%**\n" +
						$"Inset Main XP <__*insetmain*__>: **{prefs.InsetMainXP}**")
						.SendEmbed(Context.Channel);
					return;
			}
			await LevelingDB.SaveChangesAsync();
		}
	}
}

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
using Discord.Net;
using Discord.WebSocket;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Dexter.Configurations;
using System.Text;
using System.Text.RegularExpressions;
using Dexter.Helpers;

namespace Dexter.Commands
{
	public partial class UtilityCommands
	{

		/// <summary>
		/// Sets the role color of users with the permission or displays the list of available color roles.
		/// </summary>
		/// <param name="colorname"></param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("color")]
		[Summary("Changes your color role or lists all available color roles.")]
		[ExtendedSummary("Changes your color role or lists all available color roles.\n" +
			"`color LIST` - displays a list of available color roles\n" +
			"`color DISPLAY [color] (, color)...` - displays a solid rectangle of the chosen color(s) (by name or hex-code).\n" +
			"`color NONE` - removes all color roles from you\n" +
			"`color [colorname]` - changes your current color role to the one specified.")]

		public async Task SupporterRolesCommand([Remainder] string colorname)
		{
			colorname = colorname.ToLower();
			if (colorname == "list")
			{
				await PrintColorOptions();
				return;
			}
			else if (colorname.StartsWith("display"))
            {
				if (colorname == "display")
                {
					await PrintColorOptions();
					return;
				}

				string[] preprocessedColors = colorname["display".Length..].Split(',', StringSplitOptions.TrimEntries);
				List<System.Drawing.Color> colors = ProcessColorNames(preprocessedColors, out List<string> errors);

				if (errors.Any())
                {
					await Context.Channel.SendMessageAsync(string.Join("\n", errors));
                }

				if (colors.Any())
                {
					await DisplayColors(colors);
                }
				return;
            }

			Discord.Rest.RestGuildUser user = await DiscordShardedClient.Rest.GetGuildUserAsync(BotConfiguration.GuildID, Context.User.Id);
			if (user is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to find user!")
					.WithDescription("This could be due to caching errors, but I couldn't find you in the server! Try again later, if this persists, contact the developer team or an administrator.")
					.SendEmbed(Context.Channel);
				return;
			}

			IEnumerable<IRole> roles = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).Roles.ToArray();
			Dictionary<ulong, IRole> colorRoleIDs = new();
			Dictionary<ulong, int> colorRoleTiers = new();

			IRole toAdd = null;
			foreach (IRole role in roles)
			{
				if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix))
				{
					colorRoleIDs.Add(role.Id, role);
					colorRoleTiers.Add(role.Id, GetColorRoleTier(role.Id));
					if (toAdd is null && role.Name.ToLower().EndsWith(colorname.ToLower()))
					{
						toAdd = role;
					}
				}
			}

			int colorChangePerms = GetRoleChangePerms(user);
			if (colorChangePerms < 1 || colorname == "none")
			{
				IEnumerable<IRole> removed = await TryRemoveRolesAndGet(user, colorRoleIDs);
				if (removed is null) return;

				if (colorChangePerms < 1)
					await Context.Channel.SendMessageAsync("You don't have the necessary roles to change your color role to the selected one!");
				else
				{
					if (removed.Any())
					{
						List<string> roleNames = new();
						foreach (IRole role in removed)
							roleNames.Add(role.Name);
						await Context.Channel.SendMessageAsync($"Removed color roles! [{string.Join(", ", roleNames)}]");
					}
					else
						await Context.Channel.SendMessageAsync($"No roles found to remove!");
				}
				return;
			}

			if (toAdd is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Color not found")
					.WithDescription($"Unable to find the specified color \"{colorname}\". To see a list of colors and their names, use the `{BotConfiguration.Prefix}color list` command.")
					.SendEmbed(Context.Channel);
				return;
			}

			if (UtilityConfiguration.LockedColors.ContainsKey(toAdd.Id)
				&& !user.RoleIds.Contains(UtilityConfiguration.LockedColors[toAdd.Id]))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Locked Role!")
					.WithDescription($"In order to unlock this role, you must first get <@&{UtilityConfiguration.LockedColors[toAdd.Id]}>.")
					.SendEmbed(Context.Channel);
				return;
			}

			int requiredPerm = colorRoleTiers[toAdd.Id];
			if (colorChangePerms < requiredPerm)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Insufficient Role Privileges!")
					.WithDescription($"The role you selected requires a color access tier of {requiredPerm}; your current color access tier is {colorChangePerms}")
					.SendEmbed(Context.Channel);
				return;
			}

			if (!await TryRemoveRoles(user, colorRoleIDs)) return;
			try
			{
				await user.AddRoleAsync(toAdd, new RequestOptions { RetryMode = RetryMode.AlwaysRetry });
			}
			catch
			{
				await Context.Channel.SendMessageAsync("Unable to add role! Missing required permissions.");
				return;
			}

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Successfully added new role!")
				.WithDescription($"Changed your color role to: {toAdd.Name}.")
				.SendEmbed(Context.Channel);
		}

		private int GetColorRoleTier(ulong roleId)
		{
			return GetColorRoleTier(roleId, UtilityConfiguration);
		}

		/// <summary>
		/// Gets the color role permission tier required to change to a given role.
		/// </summary>
		/// <param name="roleId">The ID of the role to get an ID for.</param>
		/// <param name="config">The Utility configuration that determines the tier of given roles.</param>
		/// <returns>The numeric tier of the given role.</returns>

		public static int GetColorRoleTier(ulong roleId, UtilityConfiguration config)
		{
			if (config.ColorTiers.ContainsKey(roleId)) return config.ColorTiers[roleId];
			return config.LockedColors.ContainsKey(roleId) ? 2 : 3;
		}

		private async Task<bool> TryRemoveRoles(IGuildUser user, Dictionary<ulong, IRole> colorRoleIDs)
		{
			return await TryRemoveRolesAndGet(user, colorRoleIDs) is not null;
		}

		private async Task<IEnumerable<IRole>> TryRemoveRolesAndGet(IGuildUser user, Dictionary<ulong, IRole> colorRoleIDs)
		{
			List<IRole> toRemove = new();
			ulong[] userRoles = user.RoleIds.ToArray();
			foreach (ulong roleID in userRoles)
			{
				if (colorRoleIDs.ContainsKey(roleID))
				{
					toRemove.Add(colorRoleIDs[roleID]);
				}
			}

			if (!toRemove.Any()) return Array.Empty<IRole>();

			try
			{
				await user.RemoveRolesAsync(toRemove);
				return toRemove;
			}
			catch (HttpException e)
			{
				await Context.Channel.SendMessageAsync($"Missing permissions for role management! {e}");
				return null;
			}
		}

		private async Task PrintColorOptions()
		{
			string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
			string filepath = Path.Join(imageChacheDir, $"ColorsList.jpg");

			if (!File.Exists(filepath))
			{
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

		public async Task ReloadColorListCommand()
		{
			await ReloadColorList(true);
		}

		const int IconRoleNameMargin = 10;

		private async Task ReloadColorList(bool verbose = false)
		{
			List<SocketRole> roles = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).Roles.ToList();
			roles.Sort((ra, rb) => rb.Position.CompareTo(ra.Position));
			List<IRole> colorRoles = new();

			foreach (IRole role in roles)
			{
				if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix))
				{
					colorRoles.Add(role);
				}
			}

			if (verbose && !colorRoles.Any())
			{
				await Context.Channel.SendMessageAsync($"No roles found with the prefix: \"{UtilityConfiguration.ColorRolePrefix}\".");
				return;
			}

			int colwidth = UtilityConfiguration.ColorListColWidth;
			int colcount = UtilityConfiguration.ColorListColCount;
			int rowheight = UtilityConfiguration.ColorListRowHeight;
			int rowcount = (colorRoles.Count - 1) / colcount + 1;
			int height = rowcount * rowheight;

			string fontPath = Path.Join(Directory.GetCurrentDirectory(), "Images", "OtherMedia", "Fonts", "Whitney", "whitneymedium.otf");

			PrivateFontCollection privateFontCollection = new();
			privateFontCollection.AddFontFile(fontPath);
			FontFamily fontfamily = privateFontCollection.Families.First();
			Font font = new(fontfamily, UtilityConfiguration.ColorListFontSize, FontStyle.Regular, GraphicsUnit.Pixel);

			Bitmap picture = new(colwidth * colcount, height);
			using (Graphics g = Graphics.FromImage(picture))
			{
				int col = 0;
				int row = 0;

				using System.Drawing.Image icon = System.Drawing.Image.FromFile(Path.Join(Directory.GetCurrentDirectory(), "Images", "PawIcon.png"));
				foreach (IRole role in colorRoles)
				{
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

					if (UtilityConfiguration.ColorListDisplayByRows)
					{
						col++;
						if (col >= colcount)
						{
							col = 0;
							row++;
						}
					}
					else
					{
						row++;
						if (row >= rowcount)
						{
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

		private readonly static float[] alphatransform = new float[] { 0, 0, 0, 1, 0 };
		private readonly static float[] lineartransform = new float[] { 0, 0, 0, 0, 1 };

		/// <summary>
		/// Returns a color transformation matrix with the given RGB values.
		/// </summary>
		/// <param name="r">The red component value</param>
		/// <param name="g">The green component value</param>
		/// <param name="b">The blue component value</param>
		/// <returns></returns>

		public static ColorMatrix BasicTransform(float r, float g, float b)
		{
			return new ColorMatrix(new float[][] {
				new float[] {r, 0, 0, 0, 0},
				new float[] {0, g, 0, 0, 0},
				new float[] {0, 0, b, 0, 0},
				alphatransform,
				lineartransform
				});
		}

		private string ToRoleName(IRole role)
		{
			return role.Name[UtilityConfiguration.ColorRolePrefix.Length..];
		}

		private int GetRoleChangePerms(IGuildUser user)
		{
			return GetRoleChangePerms(user, UtilityConfiguration);
		}

		/// <summary>
		/// Gets the tier attached to someone's ability to set their color role.
		/// </summary>
		/// <param name="user">The user to read roles and permissions for.</param>
		/// <param name="config">The static UtilityConfiguration file which has information about roles.</param>
		/// <returns>A numeric tier of <paramref name="user"/>'s abilities to change roles.</returns>

		public static int GetRoleChangePerms(IGuildUser user, UtilityConfiguration config)
		{
			if (user is null) return 0;

			int value = 0;
			foreach (KeyValuePair<ulong, int> role in config.ColorChangeRoles)
			{
				if (value >= role.Value) continue;
				if (user.RoleIds.Contains(role.Key)) value = role.Value;
			}
			return value;
		}

		private List<System.Drawing.Color> ProcessColorNames(IEnumerable<string> names, out List<string> errors)
        {
			Dictionary<string, IRole> nameToRole = new();
			foreach (IRole role in DiscordShardedClient.GetGuild(BotConfiguration.GuildID).Roles)
            {
				if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix))
                {
					nameToRole.Add(role.Name[UtilityConfiguration.ColorRolePrefix.Length..].ToLower().Trim(), role);
                }
            }

			errors = new();
			List<System.Drawing.Color> result = new();

			foreach (string n in names)
            {
				if (nameToRole.ContainsKey(n.ToLower()))
                {
					result.Add(nameToRole[n.ToLower()].ToGraphicsColor());
					continue;
                }
				System.Drawing.Color color = System.Drawing.Color.FromName(n.Replace(" ", ""));
				if (color.A != 0)
                {
					result.Add(color);
					continue;
                }
				color = LanguageHelper.ColorFromHex(n);
				if (!color.IsEmpty)
                {
					result.Add(color);
					continue;
                }
				errors.Add($"Unable to parse color {n}");
            }

			return result;
        }

		private async Task DisplayColors(IEnumerable<System.Drawing.Color> colors)
		{
			int width = UtilityConfiguration.ColorDisplayWidth;
			int height = UtilityConfiguration.ColorDisplayHeight;
			using System.Drawing.Image img = new Bitmap(width, height);
			using (Graphics g = Graphics.FromImage(img))
			{

				int max = colors.Count();
				float uwidth = (float)width / max;
				int i = 0;
				foreach (System.Drawing.Color c in colors)
				{
					Rectangle rect = new Rectangle((int)Math.Round(uwidth * i), 0, (int)Math.Round(uwidth * (i + 1)), height);
					Brush brush = new SolidBrush(c);

					g.FillRectangle(brush, rect);
					i++;
				}
			}

			string path = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache", "ColorDisplay.png");
			img.Save(path);

			await Context.Channel.SendFileAsync(path);
			File.Delete(path);
        }
	}

}

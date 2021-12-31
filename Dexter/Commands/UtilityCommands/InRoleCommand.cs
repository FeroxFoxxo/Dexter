using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Commands
{

	public partial class UtilityCommands
	{

		/// <summary>
		/// Generates a list of all members who have a given role.
		/// </summary>
		/// <returns>A <see cref="Task"/> object, which can be awaited until this method completes successfully.</returns>

		[Command("inrole")]
		[Summary("Creates a list of all members that have a given role.")]
		[Alias("rolemembers")]
		[BotChannel]

		public async Task InRoleCommand([Remainder] string role)
		{
			if (Context.Channel is IDMChannel)
            {
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Invalid context")
					.WithDescription("This command must be used inside a guild (Discord Server).")
					.SendEmbed(Context.Channel);
				return;
            }

			SocketRole r = null;
			string m = Regex.Match(role, @"[0-9]{17,18}").Value;

			if (!string.IsNullOrEmpty(m))
            {
				r = Context.Guild.GetRole(ulong.Parse(m));

				if (r is null)
                {
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to find role")
						.WithDescription($"No role exists in Guild \"{Context.Guild.Name}\" by the given ID: {m}")
						.SendEmbed(Context.Channel);
					return;
                }
            } 
			else
            {
				role = role.ToLower().Replace(" ", "");
				foreach (SocketRole roleCheck in Context.Guild.Roles)
                {
					if (roleCheck.Name.ToLower().Replace(" ", "") == role)
                    {
						r = roleCheck;
						break;
                    }
                }

				if (r is null)
                {
					foreach (SocketRole roleCheck in Context.Guild.Roles)
					{
						string tname = roleCheck.Name.ToLower().Replace(" ", "");
						if (tname.StartsWith(role) || role.StartsWith(tname))
						{
							r = roleCheck;
							break;
						}
					}
				}

				if (r is null)
                {
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to find role")
						.WithDescription($"No role exists in Guild \"{Context.Guild.Name}\" by the given simplified role name: {role}")
						.SendEmbed(Context.Channel);
					return;
				}
            }

			IGuildUser[] users = r.Members.ToArray();
			int count = users.Length;

			if (count == 0)
            {
				await BuildEmbed(EmojiEnum.Sign)
					.WithTitle("No users!")
					.WithDescription("No users have this role.")
					.SendEmbed(Context.Channel);
				return;
            }

			List<EmbedBuilder> embeds = new();
			List<string> sublists = new();

			int page = 0;
			int maxpages = Math.Min(UtilityConfiguration.InRoleMaxPages, 1 + (count - 1) / UtilityConfiguration.InRoleMaxItemsPerPage);
			int itemsRemaining = 0;
			StringBuilder b = new();
			foreach (IGuildUser user in users)
            {
				if (itemsRemaining <= 0)
                {
					page++;
					itemsRemaining = UtilityConfiguration.InRoleMaxItemsPerPage;
					if (b.Length > 0)
                    {
						sublists.Add(b.ToString());
						b.Clear();
                    }

					if (page > maxpages) break;
                }

				b.Append($"\n{user.Mention} - {user.Username}");
				itemsRemaining--;
            }
			if (b.Length > 0)
			{
				sublists.Add(b.ToString());
			}

			page = 1;
			foreach(string sublist in sublists)
            {
				embeds.Add(new EmbedBuilder()
					.WithColor(Color.Blue)
					.WithTitle($"{r.Name} - Page {page++}/{maxpages}")
					.WithDescription($"{count} users with the role {r.Mention}" + sublist));
			}

			await CreateReactionMenu(embeds.ToArray(), Context.Channel);
		}

	}

}

﻿using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Commands
{

	public partial class ModeratorCommands
	{

		/// <summary>
		/// The Delete Infraction method runs on UNMUTE. It sets a record to a revoked status,
		/// making it so that the record is thus removed from an individual and cannot be seen through the records command.
		/// </summary>
		/// <param name="User">The ID user you want the infraction removed from.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("unmute")]
		[Summary("Removes an infraction from a specified user based on the infraction's ID.")]
		[Alias("delmute")]
		[RequireModerator]

		public async Task DeleteInfraction(IGuildUser User)
		{
			DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

			DexterProfile.CurrentPointTimer = string.Empty;

			await ModerationService.RemoveMutedRole(new() { { "UserID", User.Id.ToString() } });

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle($"Successfully Unmuted {User.Username}.")
				.WithDescription($"Heya! I have successfully unmuted {User.GetUserInformation()}. Give them a headpat. <3")
				.SendEmbed(Context.Channel);

			await InfractionsDB.EnsureSaved();
		}

		/// <summary>
		/// The Delete Infraction method runs on DELRECORD. It sets a record to a revoked status,
		/// making it so that the record is thus removed from an individual and cannot be seen through the records command.
		/// </summary>
		/// <param name="InfractionID">The ID of the infraction that you wish to remove from the user.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("delrecord")]
		[Summary("Removes an infraction from a specified user based on the infraction's ID.")]
		[Alias("unmute", "unwarn", "unban", "delwarn", "delmute", "delban")]
		[RequireModerator]

		public async Task DeleteInfraction([Optional] int InfractionID)
		{
			if (InfractionID <= 0)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to revoke infraction!")
					.WithDescription("To revoke infractions, please enter in its Infraction ID. " +
					"This is found through using the ~records command on the user.")
					.SendEmbed(Context.Channel);

				return;
			}

			Infraction Infraction = InfractionsDB.Infractions.Find(InfractionID);

			Infraction.EntryType = EntryType.Revoke;

			SocketGuildUser Issuer = Context.Guild.GetUser(Infraction.Issuer);
			SocketGuildUser Warned = Context.Guild.GetUser(Infraction.User);

			DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(Infraction.User);

			DexterProfile.InfractionAmount += Infraction.PointCost;

			if (DexterProfile.InfractionAmount > ModerationConfiguration.MaxPoints)
				DexterProfile.InfractionAmount = ModerationConfiguration.MaxPoints;

			if (Infraction.PointCost > 2)
				await ModerationService.RemoveMutedRole(new() { { "UserID", Infraction.User.ToString() } });

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle($"Infraction Revoked! New Points: {DexterProfile.InfractionAmount}.")
				.WithDescription($"Heya! I revoked an infraction issued from {(Warned == null ? $"Unknown ({Infraction.User})" : Warned.GetUserInformation())}")
				.AddField("Issued by", Issuer == null ? $"Unknown ({Infraction.Issuer})" : Issuer.GetUserInformation())
				.AddField("Revoked by", Context.User.GetUserInformation())
				.AddField("Reason", Infraction.Reason)
				.SendEmbed(Context.Channel);

			await InfractionsDB.EnsureSaved();
		}

	}

}

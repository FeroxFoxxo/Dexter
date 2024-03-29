﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Commands
{

	public partial class GreetFurCommands
	{

		/// <summary>
		/// Displays list in descending order of the GreetFurs in The Big Picture spreadsheet database.
		/// </summary>
		/// <remarks>GreetFur count limit: 20</remarks>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("gflevels")]
		[Summary("Returns a leaderboard of all the GreetFurs and their activity levels.")]
		[Alias("gfleaderboard")]
		[BotChannel]
		[RequireGreetFur]

		public async Task GreetFurLeaderboard()
		{
			using SheetsService sheet = new(new BaseClientService.Initializer()
			{
				HttpClientInitializer = GreetFurService.ServiceProvider.GetService<UserCredential>(),
				ApplicationName = Context.Client.CurrentUser.Username,
			});

			Spreadsheet Spreadsheet = await sheet.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

			Sheet TheBigPicture = Spreadsheet.Sheets
				.Where(Sheet => Sheet.Properties.Title == GreetFurConfiguration.TheBigPictureSpreadsheet)
				.FirstOrDefault();

			ValueRange Columns = await sheet.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
				$"{TheBigPicture.Properties.Title}!A2:{GreetFurConfiguration.TotalID}{TheBigPicture.Properties.GridProperties.RowCount}")
				.ExecuteAsync();

			Dictionary<int, List<string>> GreetFurLeaderboard = new();

			for (int Index = 0; Index < Columns.Values.Count; Index++)
			{
				int Total = int.Parse(Columns.Values[Index][^1].ToString());

				if (!GreetFurLeaderboard.ContainsKey(Total))
					GreetFurLeaderboard.Add(Total, new List<string>());

				GreetFurLeaderboard[Total].Add(Columns.Values[Index][0].ToString());
			}

			List<string> GreetFurs = new();

			foreach (KeyValuePair<int, List<string>> GreetFur in GreetFurLeaderboard.OrderByDescending(Key => Key.Key))
				foreach (string GreetFurName in GreetFur.Value)
					if (!string.IsNullOrEmpty(GreetFurName))
						GreetFurs.Add($"{GreetFurs.Count + 1}. {GreetFurName} - `{GreetFur.Key}`\n");

			List<EmbedBuilder> EmbedList = new();

			for (int Embeds = 0; Embeds < Math.Ceiling(Convert.ToDecimal(GreetFurs.Count) / 20); Embeds++)
			{
				string Description = string.Empty;

				for (int i = 0; i < 20 && i < GreetFurs.Count - Embeds * 20; i++)
					Description += GreetFurs[i + Embeds * 20];

				EmbedList.Add(
					BuildEmbed(EmojiEnum.Love)
					.WithTitle("GreetFur Leaderboard.")
					.WithDescription(Description)
				);
			}

			await CreateReactionMenu(EmbedList.ToArray(), Context.Channel);
		}

	}

}

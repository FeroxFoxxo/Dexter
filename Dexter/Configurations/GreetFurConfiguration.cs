﻿using System.Collections.Generic;
using Dexter.Abstractions;

namespace Dexter.Configurations
{

	/// <summary>
	/// The GreetFurConfiguration specifies the information relating to the Google Sheets data required.
	/// </summary>

	public class GreetFurConfiguration : JSONConfig
	{
		/// <summary>
		/// The SpreadSheetID is the ID of the GreetFur spreadsheet chart.
		/// </summary>

		public string SpreadSheetID { get; set; }

		/// <summary>
		/// The FortnightSpreadsheet represents the title of the spreadsheet containing all the fortnightly data for the GreetFur.
		/// </summary>

		public string FortnightSpreadsheet { get; set; }

		/// <summary>
		/// The TheBigPictureSpreadsheet represents the title of the spreadsheet containing all the records for the GreetFurs.
		/// </summary>

		public string TheBigPictureSpreadsheet { get; set; }

		/// <summary>
		/// Holds the column name of the column in The Big Picture which holds usernames (Tags).
		/// </summary>

		public string TheBigPictureNames { get; set; }

		/// <summary>
		/// Holds the column name of the column in The Big Picture which holds user IDs.
		/// </summary>

		public string TheBigPictureIDs { get; set; }

		/// <summary>
		/// The maximum amount of weeks that The Big Picture holds.<br/>
		/// Any attempt to write data of weeks beyond this cap to The Big Picture will result in the process being aborted.
		/// </summary>

		public int TheBigPictureWeekCap { get; set; }

		/// <summary>
		/// The IDColumnIndex is the index of the column that contains all the UserIDs.
		/// </summary>

		public string IDColumnIndex { get; set; }

		/// <summary>
		/// The TotalID is the index of the column that contains all the total amounts for the Big Picture spreadsheet.
		/// </summary>

		public string TotalID { get; set; }

		/// <summary>
		/// The minimum amount of GreetFur messages in MNG for a GreetFur to have been considered active.
		/// </summary>

		public int GreetFurMinimumDailyMessages { get; set; }

		/// <summary>
		/// Whether to consider a GreetFur active if they have muted someone within a day.
		/// </summary>

		public bool GreetFurActiveWithMute { get; set; }

		/// <summary>
		/// The regular expression pattern that matches GreetFur-specific mutes.
		/// </summary>

		public string GreetFurMutePattern { get; set; }

		/// <summary>
		/// The channel of which a mute is sent into if a GreetFur triggers it.
		/// </summary>

		public ulong GreetFurMuteChannel { get; set; }

		/// <summary>
		/// The name of the webhook that is used to send GreetFur mutes into the related channel.
		/// </summary>

		public string GreetFurMuteWebhookName { get; set; }

		/// <summary>
		/// The first day since UNIX time that tracking for GreetFur activity started (defines week 1)
		/// </summary>

		public int FirstTrackingDay { get; set; }

		/// <summary>
		/// The Information dictionary stores all the column indexes and their respective names.
		/// </summary>

		public Dictionary<string, int> Information { get; set; }

		/// <summary>
		/// The Cells dictionary stores all relevant individual cells in the spreadsheet.
		/// </summary>

		public Dictionary<string, string> Cells { get; set; }

		/// <summary>
		/// Contains the general template for new rows in the fortnight sheet.
		/// </summary>

		public Dictionary<int, string> FortnightTemplates { get; set; }

		/// <summary>
		/// Contains the general template for new rows in the Big Picture sheet.
		/// </summary>

		public Dictionary<int, string> TBPTemplate { get; set; }

		/// <summary>
		/// The AWOO role ID is used for finding if a GreetFur is attempting to mute someone already in the server.
		/// </summary>

		public ulong AwooRole { get; set; }

		/// <summary>
		/// The OWO role ID is used for finding if a moderator is trying to ban a user already with the OwO role.
		/// </summary>

		public ulong OwORole { get; set; }
	}

}

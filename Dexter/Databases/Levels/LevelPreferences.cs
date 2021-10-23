using System;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Levels
{
	/// <summary>
	/// Extra user-specific data on how they wish to have their level displayed.
	/// </summary>

	[Serializable]
	public class LevelPreferences
	{

		/// <summary>
		/// The unique identifier of the user this object corresponds to.
		/// </summary>

		[Key]
		public ulong UserId { get; set; }

		/// <summary>
		/// The color to display the XP in expressed as a raw RGB value.
		/// </summary>

		public ulong XpColor { get; set; } = 0xff70cefe;

		/// <summary>
		/// The background image for the rank card.
		/// </summary>

		public string Background { get; set; } = "default";

		/// <summary>
		/// Whether to render a circular border around the user's profile picture.
		/// </summary>

		public bool PfpBorder { get; set; } = true;

		/// <summary>
		/// Whether to crop the profile picture into a circle.
		/// </summary>

		public bool CropPfp { get; set; } = true;

		/// <summary>
		/// Whether to render a black background behind the level and name.
		/// </summary>

		public bool TitleBackground { get; set; } = true;

		/// <summary>
		/// Whether to display the hybrid level bars in Dexter Merge Mode.
		/// </summary>

		public bool ShowHybrid { get; set; } = true;

		/// <summary>
		/// Dictates the opacity level of the level bar template backgrounds.
		/// </summary>

		public float LevelOpacity { get; set; } = 1;

		/// <summary>
		/// Whether to display the XP pertaining to the main level inside the XP bar (such as in hybrid mode).
		/// </summary>

		public bool InsetMainXP { get; set; } = true;
	}

}

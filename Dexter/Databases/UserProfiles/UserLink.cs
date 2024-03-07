using System;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dexter.Databases.UserProfiles
{

	/// <summary>
	/// Represents a social connection between two users
	/// </summary>

	public class UserLink
	{

		/// <summary>
		/// Represents the unique ID assigned to this link.
		/// </summary>

		[Key]
		public ulong ID { get; set; }

		/// <summary>
		/// The type of link this object represents.
		/// </summary>

		public LinkType LinkType { get; set; }

		/// <summary>
		/// The ID of the user that effected this link.
		/// </summary>

		public ulong Sender { get; set; }

		/// <summary>
		/// The ID of the user affected by this link.
		/// </summary>

		public ulong Sendee { get; set; }

		/// <summary>
		/// Holds all relevant preferences for this specific user link.
		/// </summary>

		[NotMapped]
		public LinkPreferences Settings
		{
			get
			{
				try
				{
					return JsonConvert.DeserializeObject<LinkPreferences>(SettingsStr);
				}
				catch
				{
					return null;
				}
			}
			set
			{
				SettingsStr = JsonConvert.SerializeObject(value);
			}
		}

		/// <summary>
		/// The stringified representation of the settings object.
		/// </summary>

		public string SettingsStr { get; set; }

		/// <summary>
		/// Checks whether a given <paramref name="user"/> is blocked in this link.
		/// </summary>
		/// <param name="user">The user to query for.</param>
		/// <returns><see langword="true"/> if the user is blocked, otherwise <see langword="false"/>.</returns>

		public bool IsUserBlocked(ulong user)
		{
			return LinkType == LinkType.Blocked && (
				Sender == user && Settings.BlockMode.HasFlag(Direction.Sender)
				|| Sendee == user && Settings.BlockMode.HasFlag(Direction.Sendee));
		}

		/// <summary>
		/// Blocks a given user in this link.
		/// </summary>
		/// <remarks>Only works on links of type <see cref="LinkType.Blocked"/></remarks>
		/// <param name="user">The user to block.</param>
		/// <returns><see langword="true"/> if the block is successful, otherwise <see langword="false"/>.</returns>

		public bool BlockUser(ulong user)
		{
			if (LinkType != LinkType.Blocked)
            {
                return false;
            }

            if (Sender == user) { Settings.BlockMode |= Direction.Sender; return true; }
			else if (Sendee == user) { Settings.BlockMode |= Direction.Sendee; return true; }
			else
            {
                return false;
            }
        }

		/// <summary>
		/// Checks whether a given user is set to receive birthday notifications based on this link
		/// </summary>
		/// <param name="user">The user to query for</param>
		/// <returns><see langword="true"/> if the user should receive notifications, otherwise <see langword="false"/>.</returns>

		public bool IsUserBorkdayNotified(ulong user)
		{
			return LinkType == LinkType.Friend && (
				Sender == user && Settings.BorkdayMode.HasFlag(Direction.Sender)
				|| Sendee == user && Settings.BorkdayMode.HasFlag(Direction.Sendee));
		}

		/// <summary>
		/// Sets a user up to receive birthday notifications.
		/// </summary>
		/// <remarks>Only works on links of type <see cref="LinkType.Friend"/></remarks>
		/// <param name="user">The user to set notification preferences for.</param>
		/// <returns><see langword="true"/> if the set is successful, otherwise <see langword="false"/>.</returns>

		public bool SetBorkdayNotified(ulong user)
		{
			if (LinkType != LinkType.Friend)
            {
                return false;
            }

            if (Sender == user) { Settings.BorkdayMode |= Direction.Sender; return true; }
			else if (Sendee == user) { Settings.BorkdayMode |= Direction.Sendee; return true; }
			else
            {
                return false;
            }
        }

		/// <summary>
		/// Sets a user up not to receive birthday notifications.
		/// </summary>
		/// <remarks>Only works on links of type <see cref="LinkType.Friend"/></remarks>
		/// <param name="user">The user to set notification preferences for.</param>
		/// <returns><see langword="true"/> if the set is successful, otherwise <see langword="false"/>.</returns>

		public bool ClearBorkdayNotified(ulong user)
		{
			if (LinkType != LinkType.Friend)
            {
                return false;
            }

            if (Sender == user) { Settings.BorkdayMode &= ~Direction.Sender; return true; }
			else if (Sendee == user) { Settings.BorkdayMode &= ~Direction.Sendee; return true; }
			else
            {
                return false;
            }
        }

	}

	/// <summary>
	/// Indicates which type of link a userLink represents.
	/// </summary>

	public enum LinkType : byte
	{

		/// <summary>
		/// Represents a friend request from one user to another.
		/// </summary>

		FriendRequest,

		/// <summary>
		/// Indicates that a user is a friend of another
		/// </summary>

		Friend,

		/// <summary>
		/// Blocks friend requests from one specific user
		/// </summary>

		Blocked,

		/// <summary>
		/// Auxiliary value used for temporary manipulation of requests.
		/// </summary>

		Invalid

	}

	/// <summary>
	/// Configures specific per-link preferences for a given link.
	/// </summary>

	public class LinkPreferences
	{

		/// <summary>
		/// Inficates which users to send birthday notifications to.
		/// </summary>

		public Direction BorkdayMode { get; set; } = Direction.Both;

		/// <summary>
		/// Indicates which direction the block relates the sender to the sendee in.
		/// </summary>

		public Direction BlockMode { get; set; } = Direction.None;

		/// <summary>
		/// Sets the borkday mode of this setting given the link it belongs to.
		/// </summary>
		/// <param name="link">The link this <see cref="LinkPreferences"/> object belongs to</param>
		/// <param name="user">The user to set preferences for</param>
		/// <param name="value">The value to set the preference to</param>
		/// <returns><see langword="true"/> if the setting was successful, otherwise <see langword="false"/>.</returns>

		public bool SetBorkdayMode(UserLink link, ulong user, bool value)
		{
			if (link.LinkType != LinkType.Friend)
            {
                return false;
            }

            if (link.Sender == user)
			{
				if (value) { BorkdayMode |= Direction.Sender; return true; }
				else { BorkdayMode &= ~Direction.Sender; return true; }
			}
			else if (link.Sendee == user)
			{
				if (value) { BorkdayMode |= Direction.Sendee; return true; }
				else { BorkdayMode &= ~Direction.Sendee; return true; }
			}
			else
            {
                return false;
            }
        }

	}

	/// <summary>
	/// Indicates how to interpret the direction attached to an effect.
	/// </summary>

	[Flags]
	public enum Direction
	{
		/// <summary>
		/// Neither user is affected
		/// </summary>
		None,
		/// <summary>
		/// Sender is affected
		/// </summary>
		Sender,
		/// <summary>
		/// Sendee is affected
		/// </summary>
		Sendee,
		/// <summary>
		/// Both users are affected
		/// </summary>
		Both
	}
}

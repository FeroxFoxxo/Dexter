using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.CommunityEvents
{

	/// <summary>
	/// Holds and manages the events suggested by members of the community for approval, modification, and release.
	/// </summary>

	public class CommunityEventsDB : Database
	{

		/// <summary>
		/// Holds every individual event that has been suggested into the system.
		/// </summary>

		public DbSet<CommunityEvent> Events { get; set; }

	}
}

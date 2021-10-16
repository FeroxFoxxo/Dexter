using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.UserProfiles
{

    /// <summary>
    /// The BorkdayDB contains a set of all the users who have had the borkday role and the time of the issue.
    /// </summary>

    public class ProfilesDB : Database
    {

        /// <summary>
        /// A table of the borkday times in the BorkdayDB database.
        /// </summary>

        public DbSet<UserProfile> Profiles { get; set; }

        /// <summary>
        /// Holds all recorded nickname and username changes.
        /// </summary>

        public DbSet<NameRecord> Names { get; set; }

        /// <summary>
        /// Holds all information related to user interactions.
        /// </summary>

        public DbSet<UserLink> Links { get; set; }

        /// <summary>
        /// Fetches a profile from the database or creates a new one if none exists for the given UserID (filled with default values).
        /// </summary>
        /// <param name="userID">The ID of the profile to fetch.</param>
        /// <returns>A UserProfile object detailing the relevant information about the user.</returns>

        public UserProfile GetOrCreateProfile(ulong userID)
        {
            UserProfile profile = Profiles.Find(userID);

            if (profile is null)
            {
                profile = new()
                {
                    UserID = userID,
                    BorkdayTime = 0,
                    DateJoined = 0,
                    TimeZone = "UTC+0:00",
                    TimeZoneDST = "UTC+0:00",
                    Settings = new()
                };

                Profiles.Add(profile);
            }

            return profile;
        }

        /// <summary>
        /// Attempts to find an existing relationship link between two users
        /// </summary>
        /// <param name="sender">The sender of the link to find or create.</param>
        /// <param name="sendee">The sendee of the link to find or create.</param>
        /// <param name="linkType">The type to set the new link type in case it is created.</param>
        /// <param name="anyOrder">Whether to try find a link where the sender and sendee are reversed.</param>
        /// <returns>A new or already tracked UserLink object with the appropriate sender and sendee.</returns>

        public UserLink GetOrCreateLink(ulong sender, ulong sendee, LinkType linkType, bool anyOrder = true)
        {

            UserLink link = Links.AsQueryable().Where(l => l.Sender == sender && l.Sendee == sendee).FirstOrDefault();
            if (link is not null) return link;

            if (anyOrder)
            {
                link = Links.AsQueryable().Where(l => l.Sender == sendee && l.Sendee == sender).FirstOrDefault();
                if (link is not null) return link;
            }

            link = new UserLink()
            {
                Sender = sender,
                Sendee = sendee,
                LinkType = linkType,
                Settings = new()
            };
            Links.Add(link);

            return link;
        }

        /// <summary>
        /// Gets a link given a set of parameters.
        /// </summary>
        /// <param name="sender">The Sender of the link to query for.</param>
        /// <param name="sendee">The Sendee of the link to query for.</param>
        /// <param name="anyOrder">Whether the <paramref name="sender"/> and <paramref name="sendee"/> are interchangeable.</param>
        /// <param name="linkType">The type of link to look for, only has an effect if <paramref name="strictLinkType"/> = <see langword="true"/>.</param>
        /// <param name="strictLinkType">Whether to look exclusively for links of type <paramref name="linkType"/>. If no links of <paramref name="linkType"/> are found, the result will be <see langword="null"/>.</param>
        /// <returns>A <see cref="UserLink"/> with the given properties, or <see langword="null"/> if none exist with the given parameters.</returns>

        public UserLink GetLink(ulong sender, ulong sendee, bool anyOrder = true, bool strictLinkType = false, LinkType linkType = LinkType.Friend)
        {
            UserLink link = Links.AsQueryable().Where(l => l.Sender == sender && l.Sendee == sendee && (!strictLinkType || l.LinkType == linkType)).FirstOrDefault();
            if (link is not null) return link;

            if (anyOrder)
            {
                link = Links.AsQueryable().Where(l => l.Sender == sendee && l.Sendee == sender && (!strictLinkType || l.LinkType == linkType)).FirstOrDefault();
                if (link is not null) return link;
            }

            return link;
        }

        /// <summary>
        /// Gets all users linked to a given <paramref name="user"/> by a given <paramref name="linkType"/>. 
        /// </summary>
        /// <param name="user">The user to look for connections with.</param>
        /// <param name="mustBeSender">Whether to ignore any cases where the user is the sendee of the link.</param>
        /// <param name="linkType">The type of link to filter for.</param>
        /// <returns>A list of user IDs for users that match the above criteria.</returns>

        public async Task<List<ulong>> GetLinksAsync(ulong user, bool mustBeSender = false, LinkType linkType = LinkType.Friend)
        {
            return await GetLinksAsync(user, (l) => true, mustBeSender, linkType);
        }

        /// <summary>
        /// Gets all users linked to a given <paramref name="user"/> by a given <paramref name="linkType"/>. 
        /// </summary>
        /// <param name="user">The user to look for connections with.</param>
        /// <param name="filter">An additional filter applied against the settings of a given link.</param>
        /// <param name="mustBeSender">Whether to ignore any cases where the user is the sendee of the link.</param>
        /// <param name="linkType">The type of link to filter for.</param>
        /// <returns>A list of user IDs for users that match the above criteria.</returns>

        public async Task<List<ulong>> GetLinksAsync(ulong user, Func<UserLink, bool> filter, bool mustBeSender = false, LinkType linkType = LinkType.Friend)
        {
            List<ulong> links = new();
            await Links.AsAsyncEnumerable().ForEachAsync(l =>
            {
                if (l.LinkType != linkType) return;
                if (l.Sender == user)
                {
                    if (filter(l))
                        links.Add(l.Sendee);
                }
                else if (!mustBeSender && l.Sendee == user)
                {
                    if (filter(l))
                        links.Add(l.Sender);
                }
            });

            return links;
        }

        /// <summary>
        /// Checks whether two users are linked by a given <paramref name="linkType"/>.
        /// </summary>
        /// <param name="sender">The Sender user to look for in the link.</param>
        /// <param name="sendee">The Sendee user to look for in the link.</param>
        /// <param name="anyOrder">Whether the order of the sender and sendee can be altered.</param>
        /// <param name="linkType">What type of link to look for.</param>
        /// <returns><see langword="true"/> if a link is found with the given parameters, otherwise <see langword="false"/>.</returns>

        public async Task<bool> AreLinked(ulong sender, ulong sendee, bool anyOrder = true, LinkType linkType = LinkType.Friend)
        {
            bool found = false;

            await Links.AsAsyncEnumerable().ForEachAsync(l =>
            {
                if (found) return;
                if (l.LinkType != linkType) return;
                if (l.Sender == sender && l.Sendee == sendee) found = true;
                else if (anyOrder && l.Sender == sendee && l.Sendee == sender) found = true;
            });

            return found;
        }

        /// <summary>
        /// Obtains a list of users set to receive birthday notifications for a given <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose birthday it is.</param>
        /// <returns>A list of ulong unique IDs identifying the set of users who should be notified.</returns>

        public async Task<List<ulong>> BorkdayNotifs(ulong user)
        {
            return await GetLinksAsync(user, (l) =>
            {
                return l.IsUserBorkdayNotified(user);
            });
        }

        /// <summary>
        /// Attempts to remove a given link from the database.
        /// </summary>
        /// <param name="sender">The sender of the link to be removed.</param>
        /// <param name="sendee">The sendee of the link to be removed.</param>
        /// <param name="anyOrder">Whether the sender and the sendee can be interchanged in the query.</param>
        /// <param name="linkType">The type of link to remove.</param>
        /// <returns><see langword="true"/> if a compatible link was found and removed, otherwise <see langword="false"/>.</returns>

        public bool TryRemove(ulong sender, ulong sendee, bool anyOrder = true, LinkType linkType = LinkType.Friend)
        {
            UserLink link = Links.AsQueryable().Where(l => l.Sender == sender && l.Sendee == sendee && l.LinkType == linkType).FirstOrDefault();

            if (link is null && anyOrder)
            {
                link = Links.AsQueryable().Where(l => l.Sender == sendee && l.Sendee == sender && l.LinkType == linkType).FirstOrDefault();
            }

            if (link is null) return false;

            Links.Remove(link);

            return true;
        }

    }

}
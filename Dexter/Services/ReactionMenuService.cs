using System.Threading.Tasks;
using Dexter.Abstractions;
using Discord;
using Discord.WebSocket;

namespace Dexter.Services
{

    /// <summary>
    /// The Reaction Menu service, which is used to create and update reaction menus.
    /// </summary>

    public class ReactionMenuService : Service
    {

        /// <summary>
        /// The Initialize method hooks the client ReactionAdded events and sets them to their related delegates.
        /// It is also used to delete the previous database to save on space.
        /// </summary>

        public override void Initialize()
        {

        }

        /// <summary>
        /// Creates a reaction menu from an array of template embeds and sets up all required database fields, then sends the embed to the target <paramref name="Channel"/>.
        /// </summary>
        /// <param name="EmbedBuilders">The template for the set of pages the ReactionMenu may display.</param>
        /// <param name="Channel">The channel to send the ReactionMenu into.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task CreateReactionMenu(EmbedBuilder[] EmbedBuilders, ISocketMessageChannel Channel)
        {

        }

    }

}

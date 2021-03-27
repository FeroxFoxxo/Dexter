using Dexter.Abstractions;
using Dexter.Databases.ReactionMenus;
using Dexter.Enums;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The Reaction Menu service, which is used to create and update reaction menus.
    /// </summary>

    public class ReactionMenuService : Service {

        /// <summary>
        /// A reference to the database holding dynamic ReactionMenu data, such as current active menus and CurrentPages attached to them.
        /// </summary>

        public ReactionMenuDB ReactionMenuDB { get; set; }

        /// <summary>
        /// The Initialize method hooks the client ReactionAdded events and sets them to their related delegates.
        /// It is also used to delete the previous database to save on space.
        /// </summary>
        
        public override void Initialize() {
            DiscordSocketClient.ReactionAdded += ReactionMenu;

            // Clear reaction menus if exists.
            string DBPath = Path.Combine(Directory.GetCurrentDirectory(), "Databases", $"{ReactionMenuDB.GetType().Name}.db");

            if (File.Exists(DBPath))
                File.Delete(DBPath);

            ReactionMenuDB.Database.EnsureCreated();
        }

        /// <summary>
        /// Performs navigation of a specific ReactionMenu when a <paramref name="Reaction"/> is added onto any message, if the target <paramref name="CachedMessage"/> corresponds to a ReactionMenu.
        /// </summary>
        /// <param name="CachedMessage">The Chached Message entity for the message the <paramref name="Reaction"/> was added to.</param>
        /// <param name="Channel">The Channel entity the caches message is in.</param>
        /// <param name="Reaction">The reaction data attached to the new reaction added to the <paramref name="CachedMessage"/>.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task ReactionMenu(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel Channel, SocketReaction Reaction) {
            ReactionMenu ReactionMenu = ReactionMenuDB.ReactionMenus.Find(CachedMessage.Id);

            if (ReactionMenu == null || Reaction.User.Value.IsBot)
                return;

            IUserMessage Message = await CachedMessage.GetOrDownloadAsync();

            EmbedBuilder[] Menus = JsonConvert.DeserializeObject<EmbedBuilder[]>
                (ReactionMenuDB.EmbedMenus.Find(ReactionMenu.EmbedMenuIndex).EmbedMenuJSON);

            if (Reaction.Emote.Name.Equals("⬅️")) {
                ReactionMenu.CurrentPage--;
                if (ReactionMenu.CurrentPage < 1)
                    ReactionMenu.CurrentPage = Menus.Length;
            } else if (Reaction.Emote.Name.Equals("➡️")) {
                ReactionMenu.CurrentPage++;
                if (ReactionMenu.CurrentPage > Menus.Length)
                    ReactionMenu.CurrentPage = 1;
            }

            await Message.ModifyAsync(MessageP => MessageP.Embed = CreateMenuEmbed(ReactionMenu));

            try {
                await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
            } catch (HttpException) { }
        }

        /// <summary>
        /// Creates a reaction menu from an array of template embeds and sets up all required database fields, then sends the embed to the target <paramref name="Channel"/>.
        /// </summary>
        /// <param name="EmbedBuilders">The template for the set of pages the ReactionMenu may display.</param>
        /// <param name="Channel">The channel to send the ReactionMenu into.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task CreateReactionMenu(EmbedBuilder[] EmbedBuilders, ISocketMessageChannel Channel) {
            RestUserMessage Message = await Channel.SendMessageAsync(
                embed: BuildEmbed(EmojiEnum.Unknown).WithTitle("Setting up reaction menu-").Build()
            );

            List<uint> Colors = new ();

            foreach (EmbedBuilder Builder in EmbedBuilders)
                Colors.Add(Builder.Color.HasValue ? Builder.Color.Value.RawValue : Color.Blue.RawValue);

            int EmbedMenuID;
            string EmbedMenuJSON = JsonConvert.SerializeObject(EmbedBuilders);

            EmbedMenu EmbedMenu = ReactionMenuDB.EmbedMenus.AsQueryable()
                .Where(Menu => Menu.EmbedMenuJSON.Equals(EmbedMenuJSON)).FirstOrDefault();

            if (EmbedMenu != null)
                EmbedMenuID = EmbedMenu.EmbedIndex;
            else {
                EmbedMenuID = ReactionMenuDB.EmbedMenus.AsQueryable().Count() + 1;

                ReactionMenuDB.EmbedMenus.Add(
                    new EmbedMenu() {
                        EmbedIndex = EmbedMenuID,
                        EmbedMenuJSON = EmbedMenuJSON
                    }
                );
            }

            int ColorMenuID;
            string ColorMenuJSON = JsonConvert.SerializeObject(Colors.ToArray());

            ColorMenu ColorMenu = ReactionMenuDB.ColorMenus.AsQueryable()
                .Where(Menu => Menu.ColorMenuJSON.Equals(ColorMenuJSON)).FirstOrDefault();

            if (ColorMenu != null)
                ColorMenuID = ColorMenu.ColorIndex;
            else {
                ColorMenuID = ReactionMenuDB.ColorMenus.AsQueryable().Count() + 1;

                ReactionMenuDB.ColorMenus.Add(new ColorMenu() {
                    ColorIndex = ColorMenuID,
                    ColorMenuJSON = ColorMenuJSON
                });
            }

            ReactionMenu ReactionMenu = new() {
                CurrentPage = 1,
                MessageID = Message.Id,
                ColorMenuIndex = ColorMenuID,
                EmbedMenuIndex = EmbedMenuID
            };

            ReactionMenuDB.ReactionMenus.Add(ReactionMenu);

            ReactionMenuDB.SaveChanges();

            await Message.ModifyAsync(MessageP => MessageP.Embed = CreateMenuEmbed(ReactionMenu));

            await Message.AddReactionAsync(new Emoji("⬅️"));
            await Message.AddReactionAsync(new Emoji("➡️"));
        }

        /// <summary>
        /// Creates the embed corresponding to the currently displayed page of the target <paramref name="ReactionMenu"/>.
        /// </summary>
        /// <param name="ReactionMenu">A specific instance of a ReactionMenu to obtain the active embed from.</param>
        /// <returns>An Embed object, which can be sent or replace a different message via editing.</returns>

        public Embed CreateMenuEmbed (ReactionMenu ReactionMenu) {
            EmbedBuilder[] Menus = JsonConvert.DeserializeObject<EmbedBuilder[]>
                (ReactionMenuDB.EmbedMenus.Find(ReactionMenu.EmbedMenuIndex).EmbedMenuJSON);

            uint[] Colors = JsonConvert.DeserializeObject<uint[]>
                (ReactionMenuDB.ColorMenus.Find(ReactionMenu.ColorMenuIndex).ColorMenuJSON);

            int CurrentPage = ReactionMenu.CurrentPage - 1;

            return Menus[CurrentPage]
                .WithColor(new Color(Colors[CurrentPage]))
                .WithFooter($"Page {ReactionMenu.CurrentPage}/{Menus.Length}")
                .WithCurrentTimestamp().Build();
        }

    }

}

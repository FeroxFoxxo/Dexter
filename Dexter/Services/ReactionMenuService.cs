using Dexter.Abstractions;
using Dexter.Databases.ReactionMenus;
using Dexter.Enums;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    public class ReactionMenuService : Service {

        public readonly List<ReactionMenu> ReactionMenus = new ();

        public override void Initialize() {
            DiscordSocketClient.ReactionAdded += ReactionMenu;
        }

        public async Task ReactionMenu(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel Channel, SocketReaction Reaction) {
            ReactionMenu Menu = ReactionMenus.Where(Menu => Menu.MessageID == CachedMessage.Id).FirstOrDefault();

            if (Menu == null || Reaction.User.Value.IsBot)
                return;

            IUserMessage Message = await CachedMessage.GetOrDownloadAsync();

            if (Reaction.Emote.Name.Equals("⬅️")) {
                Menu.CurrentPage--;
                if (Menu.CurrentPage < 1)
                    Menu.CurrentPage = Menu.EmbedMenus.Length;
            } else if (Reaction.Emote.Name.Equals("➡️")) {
                Menu.CurrentPage++;
                if (Menu.CurrentPage > Menu.EmbedMenus.Length)
                    Menu.CurrentPage = 1;
            }

            await Message.ModifyAsync(MessageP => MessageP.Embed = CreateMenuEmbed(Menu));

            await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
        }

        public async Task CreateReactionMenu(EmbedBuilder[] EmbedBuilders, ISocketMessageChannel Channel) {
            ReactionMenu ReactionMenu = new () {
                CurrentPage = 1,
                EmbedMenus = EmbedBuilders
            };

            RestUserMessage Message = await Channel.SendMessageAsync(
                embed: BuildEmbed(EmojiEnum.Unknown).WithTitle("Setting up reaction menu-").Build()
            );

            await Message.ModifyAsync(MessageP => MessageP.Embed = CreateMenuEmbed(ReactionMenu));

            ReactionMenu.MessageID = Message.Id;

            ReactionMenus.Add(ReactionMenu);

            await Message.AddReactionAsync(new Emoji("⬅️"));
            await Message.AddReactionAsync(new Emoji("➡️"));
        }

        public static Embed CreateMenuEmbed (ReactionMenu ReactionMenu) {
            return ReactionMenu.EmbedMenus[ReactionMenu.CurrentPage - 1]
                .WithFooter($"Page {ReactionMenu.CurrentPage}/{ReactionMenu.EmbedMenus.Length}")
                .WithCurrentTimestamp().Build();
        }

    }

}

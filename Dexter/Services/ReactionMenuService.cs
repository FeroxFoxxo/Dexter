using Dexter.Abstractions;
using Dexter.Databases.ReactionMenus;
using Dexter.Enums;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    public class ReactionMenuService : Service {

        public ReactionMenuDB ReactionMenuDB { get; set; }

        public override void Initialize() {
            DiscordSocketClient.ReactionAdded += ReactionMenu;
        }

        public async Task ReactionMenu(Cacheable<IUserMessage, ulong> CachedMessage, ISocketMessageChannel Channel, SocketReaction Reaction) {
            ReactionMenu ReactionMenu = ReactionMenuDB.ReactionMenus.AsQueryable().Where(Menu => Menu.MessageID == CachedMessage.Id).FirstOrDefault();

            if (ReactionMenu == null || Reaction.User.Value.IsBot)
                return;

            IUserMessage Message = await CachedMessage.GetOrDownloadAsync();

            EmbedBuilder[] Menus = JsonConvert.DeserializeObject<EmbedBuilder[]>(ReactionMenu.EmbedMenusJSON);

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

            await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
        }

        public async Task CreateReactionMenu(EmbedBuilder[] EmbedBuilders, ISocketMessageChannel Channel) {
            RestUserMessage Message = await Channel.SendMessageAsync(
                embed: BuildEmbed(EmojiEnum.Unknown).WithTitle("Setting up reaction menu-").Build()
            );

            List<uint> Colors = new ();

            foreach (EmbedBuilder Builder in EmbedBuilders)
                Colors.Add(Builder.Color.HasValue ? Builder.Color.Value.RawValue : Color.Blue.RawValue);

            ReactionMenu ReactionMenu = new() {
                CurrentPage = 1,
                EmbedMenusJSON = JsonConvert.SerializeObject(EmbedBuilders),
                MessageID = Message.Id,
                ColorMenusJSON = JsonConvert.SerializeObject(Colors.ToArray())
            };

            ReactionMenuDB.ReactionMenus.Add(ReactionMenu);

            ReactionMenuDB.SaveChanges();

            await Message.ModifyAsync(MessageP => MessageP.Embed = CreateMenuEmbed(ReactionMenu));

            await Message.AddReactionAsync(new Emoji("⬅️"));
            await Message.AddReactionAsync(new Emoji("➡️"));
        }

        public static Embed CreateMenuEmbed (ReactionMenu ReactionMenu) {
            EmbedBuilder[] Menus = JsonConvert.DeserializeObject<EmbedBuilder[]>(ReactionMenu.EmbedMenusJSON);

            uint[] Colors = JsonConvert.DeserializeObject<uint[]>(ReactionMenu.ColorMenusJSON);

            int CurrentPage = ReactionMenu.CurrentPage - 1;

            return Menus[CurrentPage]
                .WithColor(new Color(Colors[CurrentPage]))
                .WithFooter($"Page {ReactionMenu.CurrentPage}/{Menus.Length}")
                .WithCurrentTimestamp().Build();
        }

    }

}

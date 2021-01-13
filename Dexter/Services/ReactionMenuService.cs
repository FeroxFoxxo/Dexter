using Dexter.Abstractions;
using Dexter.Databases.ReactionMenus;
using Dexter.Enums;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    public class ReactionMenuService : Service {

        public ReactionMenuDB ReactionMenuDB { get; set; }

        public override void Initialize() {
            DiscordSocketClient.ReactionAdded += ReactionMenu;

            // Clear reaction menus if exists.
            string DBPath = Path.Combine(Directory.GetCurrentDirectory(), "Databases", $"{ReactionMenuDB.GetType().Name}.db");

            if (File.Exists(DBPath))
                File.Delete(DBPath);
        }

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

            await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
        }

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
            Console.WriteLine(EmbedMenuID);

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {
    public partial class LevelingCommands {

        /// <summary>
        /// Handles the modification of profile-specific level system prefrences.
        /// </summary>
        /// <param name="attribute">The attribute to modify.</param>
        /// <param name="value">The value to set the attribute to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("rankcard")]
        [Alias("configlevel")]
        [Summary("Usage: `rankcard [Attribute] (Value)` - Used to modify the appearance of your rank card.")]
        [ExtendedSummary("Usage: `rankcard [Attribute] (Value)`\n" +
            "-  `background ([DefaultImage] OR #[ColorCode])` {+Attachment} - Sets your rank card background to the `.png` or `.jpg` image you uploaded along with the command. Use the value `list` to see a list of default images.\n" +
            "-  `color ([ColorName] OR #[ColorCode])` - Sets your rank card theme color to the chosen color. You can view some examples by leaving the value field blank.\n" +
            "-  `pfpborder <true|false>` - Sets whether to render a grey circle behind your pfp in your rank card.\n" +
            "-  `croppfp <true|false>` - Sets whether to crop your pfp into a circle or render it in full in your rank card.")]
        [BotChannel]

        public async Task CustomizeLevelsCommand(string attribute = "", [Remainder] string value = "") {
            UserLevel ul = LevelingDB.GetOrCreateLevelData(Context.User.Id, out LevelPreferences prefs);

            switch (attribute.ToLower()) {
                case "color":
                case "colour":
                case "xpcolor":
                case "xpcolour":
                    if (string.IsNullOrEmpty(value)) {
                        await BuildEmbed(EmojiEnum.Sign)
                            .WithTitle("Color Information")
                            .WithDescription("You can set a custom color for your XP display. Use either the name of an existing color such as \"Red\" or a hexadecimal string such as \"#df21fe\".")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    System.Drawing.Color color;
                    color = System.Drawing.Color.FromName(value);
                    if (value.ToLower() != "black" && color.R == 0 && color.G == 0 && color.B == 0) {
                        if(!Regex.IsMatch(value, @"^(0x|#)?[0-9A-F]{6}$", RegexOptions.IgnoreCase)) {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unable to parse color!")
                                .WithDescription($"The color {value} isn't a known color name nor follows a hexadecimal format with 6 digits.")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                        int hex = int.Parse(value[^6..], System.Globalization.NumberStyles.HexNumber);
                        color = System.Drawing.Color.FromArgb(unchecked((int)(hex + 0xff000000)));
                    }
                    prefs.XpColor = (ulong)color.ToArgb();
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Success!")
                        .WithDescription($"Your rank card will now be rendered using the color: {color.Name}")
                        .SendEmbed(Context.Channel);
                    break;
                case "pfpborder":
                case "pfpbackground":
                    switch(value.ToLower()) {
                        case "true":
                        case "yes":
                        case "enabled":
                            prefs.PfpBorder = true;
                            break;
                        case "false":
                        case "no":
                        case "disabled":
                            prefs.PfpBorder = false;
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Sign)
                                .WithTitle("Information about \"pfpborder\"")
                                .WithDescription("Describes whether a gray circle should be rendered around your profile picture in your rank card.")
                                .AddField("Possible Values", "**true**: A circle will be rendered.\n**false**: A circle will not be rendered.")
                                .SendEmbed(Context.Channel);
                            return;
                    }
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Operation Successful!")
                        .WithDescription($"The value of \"pfpborder\" has been set to `{prefs.PfpBorder}`.")
                        .SendEmbed(Context.Channel);
                    break;
                case "croppfp":
                case "pfpcrop":
                    switch (value.ToLower()) {
                        case "true":
                        case "yes":
                        case "enabled":
                        case "circle":
                            prefs.CropPfp = true;
                            break;
                        case "false":
                        case "no":
                        case "disabled":
                        case "square":
                            prefs.CropPfp = false;
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Sign)
                                .WithTitle("Information about \"croppfp\"")
                                .WithDescription("Describes whether your profile picture should be cropped into a circle or remain a full square in your rank card.")
                                .AddField("Possible Values", "**true**: The picture will be cropped.\n**false**: The picture will be rendered in full.")
                                .SendEmbed(Context.Channel);
                            return;
                    }
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Operation Successful!")
                        .WithDescription($"The value of \"croppfp\" has been set to `{prefs.CropPfp}`.")
                        .SendEmbed(Context.Channel);
                    break;
                case "namebackground":
                case "namebg":
                case "toplabelbackground":
                case "toplabelbg":
                case "titlebackground":
                case "titlebg":
                    switch (value.ToLower()) {
                        case "true":
                        case "yes":
                        case "enabled":
                        case "circle":
                            prefs.TitleBackground = true;
                            break;
                        case "false":
                        case "no":
                        case "disabled":
                        case "square":
                            prefs.TitleBackground = false;
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Sign)
                                .WithTitle("Information about \"TitleBackground\"")
                                .WithDescription("Describes whether the black bar that makes your name and level generally visible should be rendered.")
                                .AddField("Possible Values", "**true**: The bar will be rendered.\n**false**: The bar won't be rendered.")
                                .SendEmbed(Context.Channel);
                            return;
                    }
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Operation Successful!")
                        .WithDescription($"The value of \"TitleBackground\" has been set to `{prefs.TitleBackground}`.")
                        .SendEmbed(Context.Channel);
                    break;
                case "background":
                case "image":
                case "bgimage":
                case "bgimg":
                case "bg":
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "Images", "Levels", "Backgrounds");
                    if (value.ToLower() == "list") {
                        string[] processedPaths = Directory.GetFiles(path);

                        for (int i = 0; i < processedPaths.Length; i++)
                            processedPaths[i] = processedPaths[i].Split('\\').Last()[..^4];
                        await BuildEmbed(EmojiEnum.Sign)
                            .WithTitle("Default Background Images")
                            .WithDescription(string.Join(", ", processedPaths))
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    if (!string.IsNullOrEmpty(value)) {
                        if (value.EndsWith(".jpg")) value = value[..^4];
                        if (value.EndsWith(".png")) value = value[..^4];

                        if (!File.Exists(Path.Combine(path, $"{value.ToLower()}.jpg"))
                            && !File.Exists(Path.Combine(path, $"{value.ToLower()}.png"))) {
                            if (Regex.IsMatch(value, @"^(0x|#)?[0-9A-F]{6}$", RegexOptions.IgnoreCase)) {
                                int hex = int.Parse(value[^6..], System.Globalization.NumberStyles.HexNumber);
                                color = System.Drawing.Color.FromArgb(unchecked((int)(hex + 0xff000000)));

                                prefs.Background = value;

                                await BuildEmbed(EmojiEnum.Love)
                                    .WithTitle("Background set to flat color!")
                                    .WithDescription($"Your rank card background will appear as the following solid color: {color.Name}")
                                    .SendEmbed(Context.Channel);
                                break;
                            }
                            string[] processedPaths = Directory.GetFiles(path);

                            for (int i = 0; i < processedPaths.Length; i++) 
                                processedPaths[i] = processedPaths[i].Split('\\').Last()[..^4];

                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unable to find default image")
                                .WithDescription($"The specified value: \"{value}\", doesn't correspond to any of the images available by default:\n" +
                                $"{string.Join(", ", processedPaths)}\n" +
                                $"Nor does it fit a color format (#FFFFFF) or a link to an image (http...)")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                        prefs.Background = value.ToLower();
                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Background Set!")
                            .WithDescription($"Your background has been set to the default image {value}.")
                            .SendEmbed(Context.Channel);
                        break;
                    }
                    if (ul.TotalLevel(LevelingConfiguration) < LevelingConfiguration.CustomImageMinimumLevel) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Locked Feature!")
                            .WithDescription($"You must reach level {LevelingConfiguration.CustomImageMinimumLevel} in order to use this feature.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    if (Context.Message.Attachments.FirstOrDefault() is null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("No Attachments Found")
                            .WithDescription($"Send an image in a valid format along with your command! It must be a single message.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    string url = Context.Message.Attachments.First().ProxyUrl;
                    if (!(url.EndsWith(".png") || url.EndsWith(".jpg"))) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid Image Format")
                            .WithDescription($"Your custom image must either have the extension `.jpg` or `.png`.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    if (Context.Message.Attachments.FirstOrDefault().Size > LevelingConfiguration.CustomImageSizeLimit) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Maximum File Size Exceeded")
                            .WithDescription($"Keep your custom image files below a size of {LevelingConfiguration.CustomImageSizeLimit} bytes.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    await (DiscordSocketClient.GetGuild(LevelingConfiguration.CustomImageDumpsGuild)
                        .GetChannel(LevelingConfiguration.CustomImageDumpsChannel) as ITextChannel)
                        .SendMessageAsync(url);
                    prefs.Background = url;
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Operation Successful!")
                        .WithDescription("Your rank card background has been set to the new image! The resolution will be adjusted accordingly.")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Sign)
                        .WithTitle("Rankcard Display Settings")
                        .WithDescription($"Here are the list of preferences you've set for your rank card display:\n" +
                        $"XPColor: #{prefs.XpColor & 0x00ffffff:X}\n" +
                        $"Background Image: {(prefs.Background.StartsWith("http") ? $"[View]({prefs.Background})" : prefs.Background)}\n" +
                        $"Pfp border: **{prefs.PfpBorder}**\n" +
                        $"Crop Pfp: **{prefs.CropPfp}**\n" +
                        $"Title Background: **{prefs.TitleBackground}**")
                        .SendEmbed(Context.Channel);
                    return;
            }
            LevelingDB.SaveChanges();
        }
    }
}

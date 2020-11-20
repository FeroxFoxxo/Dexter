using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Cooldowns;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    public class CommissionCooldownService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly CooldownDB CooldownDB;
        private readonly CommissionCooldownConfiguration CommissionCooldownConfiguration;

        public CommissionCooldownService(DiscordSocketClient DiscordSocketClient, CooldownDB CooldownDB,
                CommissionCooldownConfiguration CommissionCooldownConfiguration) {
            this.DiscordSocketClient = DiscordSocketClient;
            this.CooldownDB = CooldownDB;
            this.CommissionCooldownConfiguration = CommissionCooldownConfiguration;
        }

        public override void AddDelegates() {
            DiscordSocketClient.MessageReceived += MessageRecieved;
        }

        public async Task MessageRecieved(SocketMessage SocketMessage) {
            if (SocketMessage.Channel.Id != CommissionCooldownConfiguration.CommissionsCornerID || SocketMessage.Author.IsBot)
                return;

            Cooldown Cooldown = CooldownDB.Cooldowns.AsQueryable()
                .Where(Cooldown => Cooldown.Token.Equals($"{SocketMessage.Author.Id}{SocketMessage.Channel.Id}")).FirstOrDefault();

            if (Cooldown != null) {
                if (Cooldown.TimeOfCooldown + CommissionCooldownConfiguration.CommissionCornerCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                    if (Cooldown.TimeOfCooldown + CommissionCooldownConfiguration.GracePeriod < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                        DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(Cooldown.TimeOfCooldown);

                        await SocketMessage.DeleteAsync();

                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle($"Haiya, {SocketMessage.Author.Username}.")
                            .WithDescription($"Just a friendly reminder you are only allowed to post commissions every" +
                                $" {TimeSpan.FromSeconds(CommissionCooldownConfiguration.CommissionCornerCooldown).TotalDays} days. " +
                                $"Please take a lookie over the channel pins regarding the regulations of this channel if you haven't already <3")
                            .AddField("Last Commission Sent:", $"{CooldownTime.ToLongTimeString()}, {CooldownTime.ToLongDateString()}")
                            .WithFooter($"Times are in {(TimeZoneInfo.Local.IsDaylightSavingTime(CooldownTime) ? TimeZoneInfo.Local.DaylightName : TimeZoneInfo.Local.StandardName)}.")
                            .WithCurrentTimestamp()
                            .SendEmbed(SocketMessage.Author, SocketMessage.Channel as ITextChannel);
                    }
                } else {
                    Cooldown.TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    await CooldownDB.SaveChangesAsync();
                }
            } else {
                CooldownDB.Cooldowns.Add(
                    new Cooldown() {
                        Token = $"{SocketMessage.Author.Id}{SocketMessage.Channel.Id}",
                        TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    }
                );

                await CooldownDB.SaveChangesAsync();
            }
        }

    }

}

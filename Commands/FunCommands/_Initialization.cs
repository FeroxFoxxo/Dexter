using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;
using Dexter.Databases.FunTopics;
using System;

namespace Dexter.Commands {
    public partial class FunCommands : DiscordModule {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly FunTopicsDB FunTopicsDB;

        public FunCommands(DiscordSocketClient DiscordSocketClient, FunConfiguration FunConfiguration, FunTopicsDB FunTopicsDB) {
            this.FunConfiguration = FunConfiguration;
            this.DiscordSocketClient = DiscordSocketClient;
            this.FunTopicsDB = FunTopicsDB;
        }

    }
}

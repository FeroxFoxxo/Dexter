using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;
using Dexter.Databases.FunTopics;
using System;

namespace Dexter.Commands {
    public partial class FunCommands : DiscordModule {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient Client;
        private readonly FunTopicsDB FunTopicsDB;

        public FunCommands(DiscordSocketClient Client, FunConfiguration FunConfiguration, FunTopicsDB FunTopicsDB) {
            this.FunConfiguration = FunConfiguration;
            this.Client = Client;
            this.FunTopicsDB = FunTopicsDB;
        }

    }
}

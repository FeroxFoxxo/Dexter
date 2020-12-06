using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Services;
using Google.Apis.Sheets.v4;

namespace Dexter.Commands {

    public partial class GreetFurCommands : DiscordModule {

        public LoggingService LoggingService { get; set; }

        public GreetFurConfiguration GreetFurConfiguration { get; set; }

        private SheetsService SheetsService;

    }

}

using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Services;
using Discord;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;

namespace Dexter.Commands {
    public partial class GreetFurCommands : DiscordModule {

        private readonly SheetsService SheetsService;
        private readonly GreetFurConfiguration GreetFurConfiguration;

        public GreetFurCommands(LoggingService LoggingService, GreetFurConfiguration GreetFurConfiguration) {
            this.GreetFurConfiguration = GreetFurConfiguration;

            if (!File.Exists(GreetFurConfiguration.CredentialFile)) {
                _ = LoggingService.LogMessageAsync( new LogMessage (LogSeverity.Error, GetType().Name,
                    $"GreetFur SpreadSheet credential file {GreetFurConfiguration.CredentialFile} does not exist!"));
                return;
            }

            // Open the FileStream to the related file.
            using FileStream Stream = new (GreetFurConfiguration.CredentialFile, FileMode.Open, FileAccess.Read);

            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.

            UserCredential Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(Stream).Secrets,
                new string [1] { SheetsService.Scope.SpreadsheetsReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore(GreetFurConfiguration.TokenFile, true)
            ).Result;

            // Create Google Sheets API service.
            SheetsService = new SheetsService(new BaseClientService.Initializer() {
                HttpClientInitializer = Credential,
                ApplicationName = GreetFurConfiguration.ApplicationName,
            });
        }

    }
}

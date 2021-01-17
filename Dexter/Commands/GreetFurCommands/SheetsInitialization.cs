using Discord;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class GreetFurCommands {

        /// <summary>
        /// Sets up the service and dependencies required to access the data on Google Sheets servers for use in other commands.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task SetupGoogleSheets() {
            if (!File.Exists(GreetFurConfiguration.CredentialFile)) {
                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, GetType().Name,
                    $"GreetFur SpreadSheet credential file {GreetFurConfiguration.CredentialFile} does not exist!"));
                return;
            }

            // Open the FileStream to the related file.
            using FileStream Stream = new(GreetFurConfiguration.CredentialFile, FileMode.Open, FileAccess.Read);

            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.

            UserCredential Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(Stream).Secrets,
                new string[1] { SheetsService.Scope.SpreadsheetsReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore(GreetFurConfiguration.TokenFile, true),
                new PromptCodeReceiver()
            ).Result;

            // Create Google Sheets API service.
            SheetsService = new SheetsService(new BaseClientService.Initializer() {
                HttpClientInitializer = Credential,
                ApplicationName = GreetFurConfiguration.ApplicationName,
            });
        }

    }

}

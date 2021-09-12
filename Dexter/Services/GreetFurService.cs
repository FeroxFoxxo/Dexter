using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Databases.GreetFur;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers.Games;
using Discord;
using Discord.WebSocket;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Services;

namespace Dexter.Services
{

    /// <summary>
    /// This service manages the Dexter Games subsystem and sends events to the appropriate data structures.
    /// </summary>

    public class GreetFurService : Service
    {

        /// <summary>
        /// The database holding all relevant information about GreetFur activity history.
        /// </summary>

        public GreetFurDB GreetFurDB { get; set; }

        /// <summary>
        /// The configuration file holding all relevant GreetFur-specific configuration.
        /// </summary>

        public GreetFurConfiguration GreetFurConfiguration { get; set; }

        /// <summary>
        /// Holds critical information about specific channels that should be observed by this service.
        /// </summary>

        public MNGConfiguration MNGConfiguration { get; set; }

        /// <summary>
        /// Provides access to the console to log relevant events and errors.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// Manages the Google Sheets section of GreetFur record-keeping.
        /// </summary>

        public SheetsService sheetsService;

        private const int TRACKING_LENGTH = 14;

        /// <summary>
        /// This method is run after dependencies are initialized and injected, it manages hooking up the service to all relevant events.
        /// </summary>

        public override async void Initialize()
        {
            DiscordSocketClient.MessageReceived += HandleMessage;

            await SetupGoogleSheets();
        }

        /// <summary>
        /// Responds to a message sent by a GreetFur or staff in the relevant channels; logging activity as necessary.
        /// </summary>
        /// <param name="msg">The message the user sent.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task HandleMessage(SocketMessage msg)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (msg.Author is not IGuildUser user)
                return;

            if (user.GetPermissionLevel(DiscordSocketClient, BotConfiguration) < PermissionLevel.GreetFur)
                return;

            if (Regex.IsMatch(msg.Content, GreetFurConfiguration.GreetFurMutePattern))
            {
                GreetFurDB.AddActivity(user.Id, 0, true);
            } 
            else if (msg.Channel.Id == MNGConfiguration.MeetNGreetChannel)
            {
                GreetFurDB.AddActivity(user.Id);
            }
        }

        /// <summary>
        /// Updates the currently active activity tracking spreadsheet to include the latest relevant information about user activity.
        /// </summary>
        /// <param name="displayLastFullPeriod"></param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        public async Task UpdateRemoteSpreadsheet(bool displayLastFullPeriod = false)
        {
            Spreadsheet spreadsheet = await sheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet currentFortnight = spreadsheet.Sheets
                .Where(sheet => sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            ValueRange columns = await sheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{currentFortnight.Properties.Title}!{GreetFurConfiguration.IDColumnIndex}1:{currentFortnight.Properties.GridProperties.RowCount}")
                .ExecuteAsync();

            ulong[] ids = new ulong[columns.Values.Count()];
            int today = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() / (60 * 60 * 24));
            int daysSinceTracking = today - GreetFurConfiguration.FirstTrackingDay;

            int firstDay = today - (daysSinceTracking % TRACKING_LENGTH) - Convert.ToInt32(displayLastFullPeriod) * TRACKING_LENGTH;
            
            for (int i = 1; i < columns.Values.Count() - 1; i++)
            {
                if (ulong.TryParse(columns.Values[i][0].ToString(), out ulong result))
                {
                    ids[i] = result;
                    GreetFurRecord[] records = GreetFurDB.GetRecentActivity(result, firstDay, TRACKING_LENGTH);
                    for (int d = 0; d < TRACKING_LENGTH; d++)
                    {
                        string newValue = $"{((records[d].MessageCount > GreetFurConfiguration.GreetFurMinimumDailyMessages || (GreetFurConfiguration.GreetFurActiveWithMute && records[d].MutedUser)) ? "Y" : "N")} " +
                            $"({(records[d].MutedUser ? "M" : "")}{records[d].MessageCount})";
                        columns.Values[i][1 + d] = newValue;
                    }                    
                } 
                else
                {
                    ids[i] = 0;
                }
            }

            await sheetsService.Spreadsheets.Values.Update(columns, GreetFurConfiguration.SpreadSheetID,
                $"{currentFortnight.Properties.Title}!{GreetFurConfiguration.IDColumnIndex}1:{currentFortnight.Properties.GridProperties.RowCount}")
                .ExecuteAsync();
        }

        /// <summary>
        /// Sets up the service and dependencies required to access the data on Google Sheets servers for use in other commands.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task SetupGoogleSheets()
        {
            if (!File.Exists(GreetFurConfiguration.CredentialFile))
            {
                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, GetType().Name,
                    $"GreetFur SpreadSheet credential file {GreetFurConfiguration.CredentialFile} does not exist!"));
                return;
            }

            // Open the FileStream to the related file.
            using FileStream stream = new(GreetFurConfiguration.CredentialFile, FileMode.Open, FileAccess.Read);

            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new string[1] { SheetsService.Scope.SpreadsheetsReadonly },
                "admin",
                CancellationToken.None,
                new FileDataStore(GreetFurConfiguration.TokenFile, true),
                new PromptCodeReceiver()
            );

            // Create Google Sheets API service.
            sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = GreetFurConfiguration.ApplicationName,
            });
        }
    }
}

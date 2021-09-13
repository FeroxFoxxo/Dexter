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
using Dexter.Commands;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;
using System.Text;

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
        /// <param name="options">Request options to modify the rendering and modification of the GreetFur sheet.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        public async Task UpdateRemoteSpreadsheet(GreetFurOptions options = GreetFurOptions.None)
        {
            Spreadsheet spreadsheet = await sheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet currentFortnight = spreadsheet.Sheets
                .Where(sheet => sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            ValueRange data = await sheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{currentFortnight.Properties.Title}!A1:{currentFortnight.Properties.GridProperties.RowCount}")
                .ExecuteAsync();

            List<ulong> ids = new(data.Values.Count);
            int today = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() / (60 * 60 * 24));
            int daysSinceTracking = today - GreetFurConfiguration.FirstTrackingDay;

            int firstDay = today - (daysSinceTracking % TRACKING_LENGTH) - (int)(options & GreetFurOptions.DisplayLastFull) * TRACKING_LENGTH;
            int firstDataIndex = GreetFurConfiguration.Information["Notes"] - TRACKING_LENGTH;
            Console.Out.WriteLine("Completed initial setup");
            
            for (int i = 1; i < data.Values.Count; i++)
            {
                if (ulong.TryParse(data.Values[i][GreetFurConfiguration.Information["IDs"]].ToString(), out ulong gfid))
                {
                    ids.Add(gfid);
                    GreetFurRecord[] records = GreetFurDB.GetRecentActivity(gfid, firstDay, TRACKING_LENGTH, true);
                    int localDay = GreetFurDB.GetDayForUser(gfid);
                    for (int d = 0; d < TRACKING_LENGTH; d++)
                    {
                        if ((string)data.Values[i][firstDataIndex + d] != "Exempt")
                        {
                            string newValue = DayFormat(records[d], localDay);
                            data.Values[i][firstDataIndex + d] = newValue;
                        }
                    }
                    Console.Out.WriteLine($"Completed update for ID {gfid}");
                } 
                else
                {
                    ids.Add(0);
                }
            }
            Console.Out.WriteLine("Completed base update setup");

            UpdateRequest req = sheetsService.Spreadsheets.Values.Update(data, GreetFurConfiguration.SpreadSheetID,
                $"{currentFortnight.Properties.Title}!A1:{GreetFurCommands.IntToLetters(currentFortnight.Properties.GridProperties.ColumnCount)}{currentFortnight.Properties.GridProperties.RowCount}");
            req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await req.ExecuteAsync();

            if (options.HasFlag(GreetFurOptions.AddNewRows))
            {
                ValueRange range = new ValueRange();
                List<string[]> rows = new();

                Dictionary<ulong, List<GreetFurRecord>> newActivity = GreetFurDB.GetAllRecentActivity(firstDay, TRACKING_LENGTH);

                foreach (ulong id in ids) {
                    newActivity.Remove(id);
                } 

                bool managerToggle = true;
                for (int i = 1; i < data.Values.Count; i++)
                {
                    if (!string.IsNullOrEmpty(data.Values[i][GreetFurConfiguration.Information["ManagerList"]]?.ToString()))
                    {
                        managerToggle = !managerToggle;
                    }
                }
                Console.Out.WriteLine("Calculated manager toggle");

                int rowNumber = data.Values.Count;
                foreach(KeyValuePair<ulong, List<GreetFurRecord>> kvp in newActivity)
                {
                    GreetFurRecord[] withGaps = new GreetFurRecord[TRACKING_LENGTH];
                    int inData = 0;
                    for (int i = 0; i < TRACKING_LENGTH; i++)
                    {
                        if (kvp.Value.Count <= inData || kvp.Value[inData].Date != firstDay + i)
                        {
                            withGaps[i] = new GreetFurRecord() { Date = firstDay + i, MessageCount = 0, MutedUser = false, RecordId = 0, UserId = kvp.Key };
                        } 
                        else
                        {
                            withGaps[i] = kvp.Value[inData++];
                        }   
                    }
                    rows.Add(RowFromRecords(withGaps, rowNumber++, managerToggle));
                }

                range.Values = rows.ToArray();
                AppendRequest appendReq = sheetsService.Spreadsheets.Values.Append(range, GreetFurConfiguration.SpreadSheetID, GreetFurConfiguration.FortnightSpreadsheet);
                await appendReq.ExecuteAsync();
            }
        }

        /// <summary>
        /// Changes the request options for a spreadsheet update.
        /// </summary>

        [Flags]
        public enum GreetFurOptions
        {
            /// <summary>
            /// Represents all standard options.
            /// </summary>
            None = 0,
            /// <summary>
            /// Represents the last fully-recorded historical period as opposed to the current one.
            /// </summary>
            DisplayLastFull = 1,
            /// <summary>
            /// Whether to add rows for IDs represented in the history that are not represented in the spreadsheet.
            /// </summary>
            AddNewRows = 2
        }

        private string[] RowFromRecords(GreetFurRecord[] records, int row, bool managerToggle = false)
        {
            ulong id = records[0].UserId;
            int day = GreetFurDB.GetDayForUser(id);
            string[] result = new string[records.Length];

            int firstcol = GreetFurConfiguration.Information["Notes"] - records.Length;
            for (int i = firstcol; i < firstcol + result.Length; i++)
            {
                result[i] = DayFormat(records[i], day);
            }

            foreach(int i in GreetFurConfiguration.FortnightTemplates.Keys)
            {
                result[i] = ResolveFormat(GreetFurConfiguration.FortnightTemplates[i], id, row, managerToggle);
            }

            return result;
        }

        private string DayFormat(GreetFurRecord r, int day = -1)
        {
            if (r is null)
                return "";

            if (day < 0)
            {
                day = GreetFurDB.GetDayForUser(r.UserId);
            }
            return r.ToString(GreetFurConfiguration, day);
        }

        private string ResolveFormat(string format, ulong userId, int row, bool managerToggle)
        {
            MatchCollection matches = Regex.Matches(format, @"\{[^{}]*\}");
            StringBuilder sb = new();

            int lastindex = 0;
            foreach(Match m in matches)
            {
                sb.Append(format[lastindex..m.Index]);
                lastindex = m.Index + m.Length;
                switch (m.Value)
                {
                    case "{N}":
                        sb.Append(row);
                        break;
                    case "{N+1}":
                        sb.Append(row + 1);
                        break;
                    case "{N-1}":
                        sb.Append(row - 1);
                        break;
                    case "{Id}":
                        sb.Append(userId);
                        break;
                    case "{Manager}":
                        sb.Append(managerToggle ? "TRUE" : "FALSE");
                        break;
                    case "{User}":
                    {
                        IUser u = DiscordSocketClient.GetUser(userId);
                        sb.Append(u?.Username ?? "Unknown");
                        break;
                    }
                    case "{Discriminator}":
                    {
                        IUser u = DiscordSocketClient.GetUser(userId);
                        sb.Append(u?.Discriminator ?? "????");
                        break;
                    }
                    case "{Tag}":
                    {
                        IUser u = DiscordSocketClient.GetUser(userId);
                        sb.Append(u?.Username ?? "Unknown#????");
                        break;
                    }
                }
            }
            sb.Append(format[lastindex..]);

            return sb.ToString();
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
                new string[1] { SheetsService.Scope.Spreadsheets },
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.GreetFur;
using Dexter.Enums;
using Dexter.Extensions;
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
using System.Threading;

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
        /// Manages the Google Sheets section of GreetFur record-keeping.
        /// </summary>

        public SheetsService sheetsService;

        private const int TRACKING_LENGTH = 14;

        /// <summary>
        /// This method is run after dependencies are initialized and injected, it manages hooking up the service to all relevant events.
        /// </summary>

        public override async void Initialize()
        {
            DiscordShardedClient.MessageReceived += HandleMessage;

            await SetupGoogleSheets();
        }

        /// <summary>
        /// Responds to a message sent by a GreetFur or staff in the relevant channels; logging activity as necessary.
        /// </summary>
        /// <param name="msg">The message the user sent.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        private async Task HandleMessage(SocketMessage msg)
        {
            if (msg.Author is not IGuildUser user)
                return;

            if (user.GetPermissionLevel(DiscordShardedClient, BotConfiguration) < PermissionLevel.GreetFur)
                return;

            if (user.IsBot)
                return;

            if (Regex.IsMatch(msg.Content, GreetFurConfiguration.GreetFurMutePattern))
            {
                GreetFurDB.AddActivity(user.Id, 0, ActivityFlags.MutedUser);
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
        /// <param name="week">An override for the first week to display.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        public async Task UpdateRemoteSpreadsheet(GreetFurOptions options = GreetFurOptions.None, int week = -1)
        {
            Spreadsheet spreadsheet = await sheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet currentFortnight = spreadsheet.Sheets
                .Where(sheet => sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            string dataRequestRange = $"{currentFortnight.Properties.Title}";
            string updateRangeName = $"{currentFortnight.Properties.Title}!A2:{GreetFurCommands.IntToLetters(GreetFurConfiguration.Information["Notes"])}{currentFortnight.Properties.GridProperties.RowCount}";
            ValueRange data = await sheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID, dataRequestRange)
                .ExecuteAsync();
            ValueRange toUpdate = await sheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID, updateRangeName)
                .ExecuteAsync();

            List<ulong> ids = new(toUpdate.Values.Count);
            int today = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() / (60 * 60 * 24));
            int daysSinceTracking = today - GreetFurConfiguration.FirstTrackingDay;

            int firstDay;
            if (week > 0)
            {
                firstDay = GreetFurConfiguration.FirstTrackingDay + 7 * --week;
            }
            else
            {
                firstDay = today - (daysSinceTracking % TRACKING_LENGTH) - (int)(options & GreetFurOptions.DisplayLastFull) * TRACKING_LENGTH;
            }
            int firstDataIndex = GreetFurConfiguration.Information["Notes"] - TRACKING_LENGTH;

            DateTimeOffset firstDTO = DateTimeOffset.FromUnixTimeSeconds((long)firstDay * 60 * 60 * 24);
            string firstDayName = firstDTO.ToString("MMM dd yyyy");
            string dateRangeName = $"{GreetFurConfiguration.FortnightSpreadsheet}!{GreetFurConfiguration.Cells["FirstDay"]}";
            ValueRange firstDayCell = new() {Range = dateRangeName, Values = new string[][] { new string[] { firstDayName } } };
            UpdateRequest req = sheetsService.Spreadsheets.Values.Update(firstDayCell, GreetFurConfiguration.SpreadSheetID, dateRangeName);
            req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await req.ExecuteAsync();

            Console.Out.WriteLine("Completed initial setup");
            
            for (int i = 0; i < toUpdate.Values.Count; i++)
            {
                string[] newRow = new string[firstDataIndex + TRACKING_LENGTH];
                if (ulong.TryParse(toUpdate.Values[i][GreetFurConfiguration.Information["IDs"]].ToString(), out ulong gfid))
                {
                    newRow[GreetFurConfiguration.Information["IDs"]] = gfid.ToString();
                    ids.Add(gfid);
                    GreetFurRecord[] records = GreetFurDB.GetRecentActivity(gfid, firstDay, TRACKING_LENGTH, true);
                    int localDay = GreetFurDB.GetDayForUser(gfid);

                    IUser u = DiscordShardedClient.GetUser(gfid);
                    if (u is not null)
                        newRow[GreetFurConfiguration.Information["Users"]] = $"{u.Username}#{u.Discriminator}";

                    for (int d = 0; d < TRACKING_LENGTH; d++)
                    {
                        bool isReadable = firstDataIndex + d < toUpdate.Values[i].Count;

                        string ogText = "";
                        if (isReadable)
                            ogText = toUpdate.Values[i][firstDataIndex + d] as string;
                        bool isExempt = ogText == "Exempt";
                        
                        if (options.HasFlag(GreetFurOptions.ReadExemptions))
                        {
                            if (isExempt)
                            {
                                if (!records[d].IsExempt)
                                {
                                    records[d].IsExempt = true;
                                    if (records[d].RecordId == 0)
                                    {
                                        GreetFurDB.AddActivity(gfid, 0, ActivityFlags.Exempt, records[d].Date);
                                    }
                                    else
                                    {
                                        GreetFurDB.SaveChanges();
                                    }
                                }
                            }
                            else if (records[d].IsExempt)
                            {
                                records[d].IsExempt = false;
                                GreetFurDB.SaveChanges();
                            }
                        }

                        if (options.HasFlag(GreetFurOptions.Safe) && !string.IsNullOrEmpty(ogText))
                            newRow[firstDataIndex + d] = ogText;
                        else 
                            newRow[firstDataIndex + d] = DayFormat(records[d], localDay);
                    }
                    toUpdate.Values[i] = newRow;
                } 
                else
                {
                    ids.Add(0);
                }
            }
            Console.Out.WriteLine("Completed base update setup");

            req = sheetsService.Spreadsheets.Values.Update(toUpdate, GreetFurConfiguration.SpreadSheetID, updateRangeName);
            req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await req.ExecuteAsync();

            if (options.HasFlag(GreetFurOptions.AddNewRows))
            {
                ValueRange range = new();
                List<string[]> rows = new();

                Dictionary<ulong, List<GreetFurRecord>> newActivity = GreetFurDB.GetAllRecentActivity(firstDay, TRACKING_LENGTH);

                foreach (ulong id in ids) {
                    newActivity.Remove(id);
                } 

                bool managerToggle = true;
                for (int i = 1; i < data.Values.Count; i++)
                {
                    if (data.Values[i].Count <= GreetFurConfiguration.Information["ManagerList"]
                        || string.IsNullOrEmpty(data.Values[i][GreetFurConfiguration.Information["ManagerList"]]?.ToString()))
                        break;
                    else
                        managerToggle = !managerToggle;
                }

                int rowNumber = data.Values.Count;
                foreach(KeyValuePair<ulong, List<GreetFurRecord>> kvp in newActivity)
                {
                    GreetFurRecord[] withGaps = new GreetFurRecord[TRACKING_LENGTH];
                    int inData = 0;
                    for (int i = 0; i < TRACKING_LENGTH; i++)
                    {
                        if (kvp.Value.Count <= inData || kvp.Value[inData].Date != firstDay + i)
                        {
                            withGaps[i] = new GreetFurRecord() {UserId = kvp.Key , Date = firstDay + i, MessageCount = 0, Activity = ActivityFlags.None, RecordId = 0};
                        } 
                        else
                        {
                            withGaps[i] = kvp.Value[inData++];
                        }   
                    }
                    rows.Add(RowFromRecords(withGaps, ++rowNumber, managerToggle));
                }

                foreach (string[] row in rows)
                {
                    Console.Out.WriteLine(string.Join(", ", row));
                }

                range.Values = rows.ToArray();
                AppendRequest appendReq = sheetsService.Spreadsheets.Values.Append(range, GreetFurConfiguration.SpreadSheetID, GreetFurConfiguration.FortnightSpreadsheet);
                appendReq.ValueInputOption = AppendRequest.ValueInputOptionEnum.USERENTERED;
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
            AddNewRows = 2,
            /// <summary>
            /// If enabled, the update operation will never overwrite cells that contain information.
            /// </summary>
            Safe = 4,
            /// <summary>
            /// Whether to read exemptions and log them to records while reading the sheet.
            /// </summary>
            ReadExemptions = 8
        }

        private string[] RowFromRecords(GreetFurRecord[] records, int row, bool managerToggle = false)
        {
            ulong id = records[0].UserId;
            int day = GreetFurDB.GetDayForUser(id);
            string[] result = new string[GreetFurConfiguration.FortnightTemplates.Keys.Max() + 1];

            int firstcol = GreetFurConfiguration.Information["Notes"] - records.Length;
            for (int i = firstcol; i < firstcol + records.Length; i++)
            {
                result[i] = DayFormat(records[i - firstcol], day);
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
                        IUser u = DiscordShardedClient.GetUser(userId);
                        sb.Append(u?.Username ?? "Unknown");
                        break;
                    }
                    case "{Discriminator}":
                    {
                        IUser u = DiscordShardedClient.GetUser(userId);
                        sb.Append(u?.Discriminator ?? "????");
                        break;
                    }
                    case "{Tag}":
                    {
                        IUser u = DiscordShardedClient.GetUser(userId);
                        sb.Append((u?.Username ?? "Unknown") + "#" + (u?.Discriminator ?? "????"));
                        break;
                    }
                }
            }
            sb.Append(format[lastindex..]);

            return sb.ToString();
        }

        /// <summary>
        /// Sets specific parameters when reading exeptions from the spreadsheet.
        /// </summary>

        [Flags]
        public enum ExemptionReadOptions
        {
            /// <summary>
            /// Represents a default request
            /// </summary>
            None = 0,
            /// <summary>
            /// Represents that eligible exemptions in the range that aren't logged in the spreadsheet should be removed from records.
            /// </summary>
            Remove = 1
        }

        /// <summary>
        /// Sets up the service and dependencies required to access the data on Google Sheets servers for use in other commands.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task SetupGoogleSheets()
        {
            if (!File.Exists(GreetFurConfiguration.CredentialFile))
            {
                await Debug.LogMessageAsync(
                    $"GreetFur SpreadSheet credential file {GreetFurConfiguration.CredentialFile} does not exist!",
                    LogSeverity.Error
                );
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

using System;
using System.Collections.Generic;
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
using Dexter.Commands;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Events
{

    /// <summary>
    /// This service manages the Dexter Games subsystem and sends events to the appropriate data structures.
    /// </summary>

    public class GreetFur : Event
    {

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

        public SheetsService SheetsService { get; set; }

        public ILogger<GreetFur> Logger { get; set; }

        private const int TRACKING_LENGTH = 14;

        /// <summary>
        /// This method is run after dependencies are initialized and injected, it manages hooking up the service to all relevant events.
        /// </summary>

        public override async void InitializeEvents()
        {
            DiscordShardedClient.MessageReceived += HandleMessage;
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

            using var scope = ServiceProvider.CreateScope();

            using var GreetFurDB = scope.ServiceProvider.GetRequiredService<GreetFurDB>();

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
            using var scope = ServiceProvider.CreateScope();

            using var GreetFurDB = scope.ServiceProvider.GetRequiredService<GreetFurDB>();

            Spreadsheet spreadsheet = await SheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet currentFortnight = spreadsheet.Sheets
                .Where(sheet => sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            if (currentFortnight is null)
            {
                throw new NullReferenceException($"Unable to find the activity spreadsheet (\"{GreetFurConfiguration.FortnightSpreadsheet}\"). Check the remote spreadsheet and confirm that the name is correct!");
            }

            string dataRequestRange = $"{currentFortnight.Properties.Title}";
            string updateRangeName = $"{currentFortnight.Properties.Title}!A2:{GreetFurCommands.IntToLetters(GreetFurConfiguration.Information["Notes"])}{currentFortnight.Properties.GridProperties.RowCount}";
            ValueRange data = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID, dataRequestRange)
                .ExecuteAsync();
            ValueRange toUpdate = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID, updateRangeName)
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
                week = daysSinceTracking / WEEK_LENGTH;
            }
            int firstDataIndex = GreetFurConfiguration.Information["Notes"] - TRACKING_LENGTH;

            DateTimeOffset firstDTO = DateTimeOffset.FromUnixTimeSeconds((long)firstDay * 60 * 60 * 24);
            string firstDayName = firstDTO.ToString("MMM dd yyyy");
            string dateRangeName = $"{GreetFurConfiguration.FortnightSpreadsheet}!{GreetFurConfiguration.Cells["FirstDay"]}";
            ValueRange firstDayCell = new() {Range = dateRangeName, Values = new string[][] { new string[] { firstDayName } } };
            UpdateRequest req = SheetsService.Spreadsheets.Values.Update(firstDayCell, GreetFurConfiguration.SpreadSheetID, dateRangeName);
            req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await req.ExecuteAsync();
            
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
                                }
                            }
                            else if (records[d].IsExempt)
                            {
                                records[d].IsExempt = false;
                            }
                        }

                        if (options.HasFlag(GreetFurOptions.Safe) && !string.IsNullOrEmpty(ogText))
                            newRow[firstDataIndex + d] = ogText;
                        else 
                            newRow[firstDataIndex + d] = DayFormat(records[d], GreetFurDB, localDay);
                    }
                    toUpdate.Values[i] = newRow;
                } 
                else
                {
                    ids.Add(0);
                }
            }

            req = SheetsService.Spreadsheets.Values.Update(toUpdate, GreetFurConfiguration.SpreadSheetID, updateRangeName);
            req.ValueInputOption = UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await req.ExecuteAsync();

            Dictionary<ulong, List<GreetFurRecord>> newActivity = null;
            if (options.HasFlag(GreetFurOptions.AddNewRows))
            {
                ValueRange range = new();
                List<string[]> rows = new();

                newActivity = GreetFurDB.GetAllRecentActivity(firstDay, TRACKING_LENGTH);

                bool managerToggle = true;
                for (int i = 1; i < data.Values.Count; i++)
                {
                    if (data.Values[i].Count <= GreetFurConfiguration.Information["ManagerList"]
                        || string.IsNullOrEmpty(data.Values[i][GreetFurConfiguration.Information["ManagerList"]]?.ToString()))
                        break;
                    else
                        managerToggle = !managerToggle;
                }

                HashSet<ulong> activityIDs = ids.ToHashSet();

                int rowNumber = data.Values.Count;
                foreach(KeyValuePair<ulong, List<GreetFurRecord>> kvp in newActivity)
                {
                    if (activityIDs.Contains(kvp.Key)) continue;

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
                    rows.Add(await RowFromRecords(withGaps, ++rowNumber, managerToggle));
                }

                range.Values = rows.ToArray();
                AppendRequest appendReq = SheetsService.Spreadsheets.Values.Append(range, GreetFurConfiguration.SpreadSheetID, GreetFurConfiguration.FortnightSpreadsheet);
                appendReq.ValueInputOption = AppendRequest.ValueInputOptionEnum.USERENTERED;
                await appendReq.ExecuteAsync();
            }

            if (options.HasFlag(GreetFurOptions.ManageTheBigPicture) && week + 1 < GreetFurConfiguration.TheBigPictureWeekCap)
            {
                Sheet theBigPictureSheet = spreadsheet.Sheets
                    .Where(sheet => sheet.Properties.Title == GreetFurConfiguration.TheBigPictureSpreadsheet)
                    .FirstOrDefault();

                if (theBigPictureSheet is null)
                {
                    throw new NullReferenceException($"Unable to find The Big Picture spreadsheet (\"{GreetFurConfiguration.TheBigPictureSpreadsheet}\"). Check the remote spreadsheet and confirm that the name is correct!");
                }

                int tbpRows = theBigPictureSheet.Properties.GridProperties.RowCount ?? -1;

                string tbpNamesA1 = $"{GreetFurConfiguration.TheBigPictureSpreadsheet}!{GreetFurConfiguration.TheBigPictureNames}1:{GreetFurConfiguration.TheBigPictureNames}{tbpRows}";
                string tbpIDsA1 = $"{GreetFurConfiguration.TheBigPictureSpreadsheet}!{GreetFurConfiguration.TheBigPictureIDs}1:{GreetFurConfiguration.TheBigPictureIDs}{tbpRows}";
                string tbpWeeksA1 = $"{GreetFurConfiguration.TheBigPictureSpreadsheet}!{GreetFurCommands.IntToLetters(week + GreetFurConfiguration.Information["TBPWeekStart"] - 1)}1:{GreetFurCommands.IntToLetters(week + GreetFurConfiguration.Information["TBPWeekStart"])}{tbpRows}";

                BatchGetRequest batchreq = SheetsService.Spreadsheets.Values.BatchGet(GreetFurConfiguration.SpreadSheetID);
                batchreq.Ranges = new Google.Apis.Util.Repeatable<string>(new string[] { tbpNamesA1, tbpIDsA1, tbpWeeksA1 });
                BatchGetValuesResponse batchresp = await batchreq.ExecuteAsync();
                ValueRange[] ranges = batchresp.ValueRanges.ToArray();

                List<ulong> foundIDs = new();
                for (int i = 0; i < ranges[1].Values.Count; i++)
                {
                    if (!ulong.TryParse(ranges[1].Values[i][0]?.ToString().Split('/').Last() ?? "", out ulong id) || id == 0)
                    {
                        foundIDs.Add(0);
                        continue;
                    }
                    foundIDs.Add(id);

                    GreetFurRecord[] records = GreetFurDB.GetRecentActivity(id, firstDay);
                    double[] yesPerWeek = new double[2];
                    object[] transformed = new object[2];
                    int dPerWeek = records.Length / 2;

                    for (int w = 0; w < 2; w++)
                    {
                        for (int d = 0; d < dPerWeek; d++)
                        {
                            if (records[w * dPerWeek + d].IsYes(GreetFurConfiguration))
                            {
                                yesPerWeek[w]++;
                            }
                        }
                        if (w < ranges[2].Values[i].Count && int.TryParse(ranges[2].Values[i][w].ToString(), out int n))
                            yesPerWeek[w] = Math.Max(yesPerWeek[w], n);

                        transformed[w] = yesPerWeek[w];
                    }
                    ranges[2].Values[i] = transformed;
                }

                UpdateRequest weekValuesUpdate = SheetsService.Spreadsheets.Values.Update(ranges[2], GreetFurConfiguration.SpreadSheetID, tbpWeeksA1);
                weekValuesUpdate.ValueInputOption = UpdateRequest.ValueInputOptionEnum.RAW;
                await weekValuesUpdate.ExecuteAsync();

                if (options.HasFlag(GreetFurOptions.AddNewRows))
                {
                    HashSet<ulong> tbpFoundIDs = foundIDs.ToHashSet();

                    if (newActivity is null)
                        newActivity = GreetFurDB.GetAllRecentActivity(firstDay, TRACKING_LENGTH);

                    ValueRange range = new();
                    List<string[]> newRows = new();
                    int row = ranges[1].Values.Count; 

                    foreach (ulong id in newActivity.Keys)
                    {
                        if (tbpFoundIDs.Contains(id)) continue;

                        Dictionary<int, int> activity = new();
                        foreach(GreetFurRecord record in newActivity[id])
                        {
                            if (record.IsYes(GreetFurConfiguration))
                            {
                                int recordW = (record.Date - GreetFurConfiguration.FirstTrackingDay) / WEEK_LENGTH + 1;
                                if (activity.ContainsKey(recordW)) 
                                    activity[recordW]++;
                                else 
                                    activity.Add(recordW, 1);
                            }
                        }
                        if (activity.Count > 0)
                            newRows.Add(await TBPRowFromRecords(id, activity, ++row));
                    }

                    range.Values = newRows.ToArray();
                    AppendRequest appendRequest = SheetsService.Spreadsheets.Values.Append(range, GreetFurConfiguration.SpreadSheetID, GreetFurConfiguration.TheBigPictureSpreadsheet);
                    appendRequest.ValueInputOption = AppendRequest.ValueInputOptionEnum.USERENTERED;
                    await appendRequest.ExecuteAsync();
                }
            }

            await GreetFurDB.SaveChangesAsync();
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
            ReadExemptions = 8,
            /// <summary>
            /// Whether to also update the big picture (never forced).
            /// </summary>
            ManageTheBigPicture = 16
        }

        private async Task<string[]> RowFromRecords(GreetFurRecord[] records, int row, bool managerToggle = false)
        {
            using var scope = ServiceProvider.CreateScope();

            using var GreetFurDB = scope.ServiceProvider.GetRequiredService<GreetFurDB>();

            ulong id = records[0].UserId;
            int day = GreetFurDB.GetDayForUser(id);
            string[] result = new string[GreetFurConfiguration.FortnightTemplates.Keys.Max() + 1];

            int firstCol = GreetFurConfiguration.Information["Notes"] - records.Length;
            for (int i = firstCol; i < firstCol + records.Length; i++)
            {
                result[i] = DayFormat(records[i - firstCol], GreetFurDB, day);
            }

            foreach(KeyValuePair<int, string> kvp in GreetFurConfiguration.FortnightTemplates)
            {
                result[kvp.Key] = await ResolveFormat(kvp.Value, id, row, managerToggle);
            }
            await GreetFurDB.SaveChangesAsync();

            return result;
        }

        const int WEEK_LENGTH = 7;
        private async Task<string[]> TBPRowFromRecords(ulong userId, Dictionary<int, int> activity, int row)
        {
            if (activity.Count == 0)
            {
                return null;
            }

            int firstCol = GreetFurConfiguration.Information["TBPWeekStart"];
            int length = GreetFurConfiguration.TheBigPictureWeekCap + firstCol;
            string[] result = new string[length];

            foreach (KeyValuePair<int, string> kvp in GreetFurConfiguration.TBPTemplate)
            {
                result[kvp.Key] = await ResolveFormat(kvp.Value, userId, row, false);
            }

            for (int wcol = firstCol; wcol < result.Length; wcol++)
            {
                result[wcol] = "0";
            }

            foreach(KeyValuePair<int, int> kvp in activity)
            {
                result[firstCol + kvp.Key - 1] = kvp.Value.ToString();
            }

            return result;
        }

        private string DayFormat(GreetFurRecord r, GreetFurDB GreetFurDB, int day = -1)
        {
            if (r is null)
                return "";

            if (day < 0)
            {
                day = GreetFurDB.GetDayForUser(r.UserId);
            }
            return r.ToString(GreetFurConfiguration, day);
        }

        private async Task<string> ResolveFormat(string format, ulong userId, int row, bool managerToggle)
        {
            MatchCollection matches = Regex.Matches(format, @"\{[^{}]*\}");
            StringBuilder sb = new();
            IUser u;

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
                    case "{Username}":
                        u = await DiscordShardedClient.Rest.GetUserAsync(userId);
                        sb.Append(u?.Username ?? "Unknown");
                        break;
                    case "{Discriminator}":
                        u = await DiscordShardedClient.Rest.GetUserAsync(userId);
                        sb.Append(u?.Discriminator ?? "????");
                        break;
                    case "{Tag}":
                        u = await DiscordShardedClient.Rest.GetUserAsync(userId);
                        sb.Append((u?.Username ?? "Unknown") + "#" + (u?.Discriminator ?? "????"));
                        break;
                    default:
                        sb.Append(m.Value);
                        break;
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
    }
}

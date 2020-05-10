using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HepegaTwitchBot
{
    public class HpgDocParser
    {
        static readonly string[] Scopes = {
            SheetsService.Scope.Spreadsheets
        };
        private static readonly string ApplicationName = "Hpg";
        private static readonly string SpreadsheetId = "paste_hpg_doc_id_here";
        private static SheetsService service;

        public HpgDocParser()
        {
            GoogleCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }

            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        public ParticipantInfo GetParticipantInfo(string username)
        {
            ParticipantInfo participantInfo = new ParticipantInfo();
            var rawRows = ReadEntries($"{username}!D13:J");
            for (int i = rawRows.Count - 1; i > 0; i--)
            {
                if (rawRows[i].Count != 0 && rawRows[i][0] != "" && rawRows[i][1] != "" && rawRows[i][2] != "")
                {
                    participantInfo.Section = (string) rawRows[i][0];
                    participantInfo.Game = (string) rawRows[i][1];
                    participantInfo.NominalGgp = (string) rawRows[i][3];
                    participantInfo.Events = (string) rawRows[i][6];
                    participantInfo.Events = participantInfo.Events.Replace("\n", ", ");
                    break;
                }
            }

            return participantInfo;
        }

        public string GetLeaderboard()
        {
            int userPlace = 1;
            var rawLeaderboard = ReadEntries("Таблица лидеров!B3:I8");
            string result = "";

            if (rawLeaderboard == null)
            {
                return "Не удалось найти данные";
            }

            foreach (var row in rawLeaderboard)
            {
                result += $"{userPlace}. {row[0]} [{row[7]}] ";
                userPlace++;
            }

            return result;
        }

        public string GetLastGameGgp(string username)
        {
            ParticipantInfo participantInfo = GetParticipantInfo(username);
            int ggp = Convert.ToInt32(participantInfo.NominalGgp);
            string events = participantInfo.Events;
            string procentsPattern = @"((?:\-|\+|\−)\d+)%+";
            string streakPattern = @"(?:Стрик|стрик)[\s\S]+(\+\d+)[\s\S]+"; // \+(\d{2,4})\ *
            string buhgalteryPattern = @"Бухгалтерия[\s\S]*\(+(\d+)\)+";
            string buhgalteryPattern2 = @"(?:Бухгалтерия|бухгалтерия)[\s\S]+=(\d+)";

            Regex r = new Regex(buhgalteryPattern);
            Match buhgalteryGgp = r.Match(events);
            if (buhgalteryGgp.Length != 0)
            {
                ggp = Convert.ToInt32(string.Join("", buhgalteryGgp.Value.Where(char.IsDigit)));
            }

            r = new Regex(buhgalteryPattern2);
            buhgalteryGgp = r.Match(events);
            if (buhgalteryGgp.Length != 0)
            {
                ggp = Convert.ToInt32(buhgalteryGgp.Groups[1].ToString());
            }

            r = new Regex(procentsPattern);
            MatchCollection matches = r.Matches(events);
            if (matches.Count != 0)
            {
                int procents = 100;
                foreach (Match match in matches)
                {
                    char operand = match.Value[0];
                    int.TryParse(string.Join("", match.Value.Where(char.IsDigit)), out var number);
                    if (operand == '+')
                    {
                        procents += number;
                    }
                    else
                    {
                        procents -= number;
                    }
                }

                if (procents < 50)
                {
                    procents = 50;
                }
                double multiplier = (procents / 100.0);
                ggp = Convert.ToInt32(ggp * (multiplier));
            }

            r = new Regex(streakPattern);
            Match streak = r.Match(events);
            if (streak.Length != 0)
            {
                ggp += Convert.ToInt32(streak.Value);
            }

            return "Итоговое GGP: " + ggp;
        }

        public List<string> GetAllItems()
        {
            List<string> result = new List<string>();
            var rawRows = ReadEntries("Правила 2.0!E4:F");
            foreach (var item in rawRows)
            {
                if (item.Count != 0)
                {
                    result.Add(item[0] != "" ? item[0].ToString() : item[1].ToString());
                }
            }
            return result;
        }

        static IList<IList<object>> ReadEntries(string range)
        {
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            IList<IList<object>> values;
            values = response.Values;
            if (values != null && values.Count > 0)
            {
                return values;
            }
            else
            {
                return null;
            }
        }
    }
}
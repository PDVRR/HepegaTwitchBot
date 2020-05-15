using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AngleSharp.Dom;

namespace HepegaTwitchBot
{
    public static class ParticipantsStats
    {
        private static int Delay { get; set; } = 60;
        public static string TopHpg;
        public static Dictionary<string, ParticipantInfo> ParticipantDictionary;
        private static HpgDocParser hpgDocParser;
        private static HltbParser hltbParser;
        static ParticipantsStats()
        {
            hpgDocParser = new HpgDocParser();
            hltbParser = new HltbParser();

            ParticipantDictionary = new Dictionary<string, ParticipantInfo>
            {
                {"bjorn", new ParticipantInfo { Name = "UncleBjorn" }},
                {"useless", new ParticipantInfo { Name = "UselessMouth" }},
                {"lizon", new ParticipantInfo { Name = "liz0n" }},
                {"mel", new ParticipantInfo { Name = "Melharucos" }},
                {"faker", new ParticipantInfo { Name = "Mistafaker" }},
                {"lasqa", new ParticipantInfo { Name = "Lasqa" }}
            };

            for (int i = 0; i < ParticipantDictionary.Count; i++)
            {
                UpdateParticipantInfo(ParticipantDictionary.ElementAt(i).Key);
            }
            ThreadPool.QueueUserWorkItem(o => { UpdateLeaderboard(); });
        }

        public static void SetUpdateDelay(int seconds)
        {
            Delay = seconds;
        }

        private static async void UpdateParticipantInfo(string name)
        {
            while (true)
            {
                ParticipantInfo newParticipantInfo = hpgDocParser.GetParticipantInfo(ParticipantDictionary[name].Name);
                if (newParticipantInfo.Game == ParticipantDictionary[name].Game)
                {
                    newParticipantInfo.HoursToComplete = ParticipantDictionary[name].HoursToComplete;
                    newParticipantInfo.FinalGgp = ParticipantDictionary[name].FinalGgp;
                }
                else
                {
                    string[] hltbTimes = await hltbParser.ParseGame(newParticipantInfo.Game);
                    if (hltbTimes != null)
                    {
                        newParticipantInfo.HoursToComplete = hltbTimes[0];
                    }
                    else
                    {
                        newParticipantInfo.HoursToComplete = "---";
                    }
                    newParticipantInfo.FinalGgp = hpgDocParser.GetLastGameGgp(newParticipantInfo);
                }
                ParticipantDictionary[name] = newParticipantInfo;
                Thread.Sleep(Delay * 1000);
            }
        }

        private static void UpdateLeaderboard()
        {
            while (true)
            {
                TopHpg = hpgDocParser.GetLeaderboard();
                Thread.Sleep(Delay*1000);
            }
        }
    }
}
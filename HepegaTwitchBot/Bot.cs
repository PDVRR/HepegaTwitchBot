using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using Timer = System.Timers.Timer;

namespace HepegaTwitchBot
{
    public class Bot
    {
        private int EASY_COMMAND = 3, MEDIUM_COMMAND = 5, HIGH_COMMAND = 12, SPAM_COMMAND = 1, AI_COMMAND = 4;
        TwitchClient client;
        private Anfisa anfisa;
        private HltbParser hltbParser;
        private GamefaqParser gamefaqParser;
        private CoronavirusParser coronaParser;
        private Random random;
        private string _channel;
        private int spamCount;
        private int easyCommand;
        private int mediumCommand;
        private int highCommand;
        private int spamCommand;
        private int aiCommand;
        List<string> items;
        private bool spamAllowed = false;
        private int spamSymbolCount = 390;
        private bool aiAllowed = false;
        private Dictionary<string, int> messages;
        private List<string> bannedUsers;
        string[] kanobu =
        {
            "кам",
            "нож",
            "бум"
        };

        string[] kanobuAnswer =
        {
            "камень",
            "ножницы",
            "бумага"
        };

        public bool PrintLog { get; set; } = false;
        public bool RepeatAllowed { get; set; } = false;

        public Bot(string channel)
        {
            InitializeVariables();

            _channel = channel;
            if (_channel.ToLower() == "uselessmouth")
            {
                aiAllowed = true;
                spamAllowed = true;
                SPAM_COMMAND = 1;
                AI_COMMAND = 1;
            }

            InitializeBot();
            InitializeCommandsTimer();
        }

        private void InitializeVariables()
        {
            hltbParser = new HltbParser();
            gamefaqParser = new GamefaqParser();
            coronaParser = new CoronavirusParser();
            messages = new Dictionary<string, int>();
            anfisa = new Anfisa();
            random = new Random();
            bannedUsers = new List<string>();
            items = HpgItems.Items;
        }

        private void InitializeBot()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Settings.Username, Settings.Token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, _channel);
            //client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnMessageReceived += Client_OnLog;
            client.OnMessageSent += Client_OnMessageSent;
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
        }

        private void InitializeCommandsTimer()
        {
            Timer commandsTimer = new Timer()
            {
                Interval = 1000
            };
            commandsTimer.Elapsed += Timer_Elapsed;
            commandsTimer.Start();
        }

        public void Connect()
        {
            client.Connect();
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        private void Client_OnLog(object sender, OnMessageReceivedArgs e)
        {
            if (!PrintLog)
            {
                return;
            }
            Console.WriteLine($"#{e.ChatMessage.Channel,-11} | {e.ChatMessage.Username}: {e.ChatMessage.Message}");
        }

        private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            if (!PrintLog)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"#{e.SentMessage.Channel,-11} | {Settings.Username} {e.SentMessage.Message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (easyCommand > 0)
            {
                easyCommand--;
            }

            if (mediumCommand > 0)
            {
                mediumCommand--;
            }

            if (highCommand > 0)
            {
                highCommand--;
            }

            if (spamCommand > 0)
            {
                spamCommand--;
            }

            if (aiCommand > 0)
            {
                aiCommand--;
            }
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Console.WriteLine($"Disconnected from {_channel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            client.SendMessage(e.Channel, "Привет! Я неофициальный бот HPG 2.0. Пиши !команды в чат, чтобы узнать список доступных команд.");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            string message = e.ChatMessage.Message.ToLower();
            string channel = e.ChatMessage.Channel;
            string username = e.ChatMessage.Username;
            if (message.StartsWith("!команды") && easyCommand == 0)
            {
                username = SendAMessageTo(username, ref message);
                easyCommand = EASY_COMMAND;
                client.SendMessage(channel, $"@{username} Список доступных команд: !топхпг, !хпгигра [ник_участника], !события [ник_участника], !описание [шмотка_или_событие], !hltb [игра], !gamefaq [игра], !коронавирус [страна_на_английском] (кд 12 сек), !github");
            }
            else if (message.StartsWith("!топхпг") && mediumCommand == 0)
            {
                username = SendAMessageTo(username, ref message);
                mediumCommand = MEDIUM_COMMAND;
                client.SendMessage(channel, $"@{username} {ParticipantsStats.TopHpg}");
            }
            else if (message.StartsWith("!хпгигра") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                SendGameMessage(username, message, channel);
            }
            else if (message.StartsWith("!яебу") && !message.ContainsAlias(Aliases.ProhibitedAliases) && spamCommand == 0 && spamAllowed && !bannedUsers.Contains(username))
            {
                spamCommand = SPAM_COMMAND;
                message = e.ChatMessage.Message.Replace("!яебу", "");
                SendYaEbuMessage(message, channel);
            }
            else if (message.StartsWith("!запретить спам") && (username == "pdvrr" || username == "martellx" || e.ChatMessage.UserType == UserType.Moderator))
            {
                spamAllowed = false;
                client.SendMessage(channel, "Спам запрещен :)");
            }
            else if (message.StartsWith("!запретить") && (username == "pdvrr" || username == "martellx" || e.ChatMessage.UserType == UserType.Moderator))
            {
                message = message.Replace("!запретить ", "");
                bannedUsers.Add(message);
                client.SendMessage(channel, $"{message} запрещено спамить :)");
            }
            else if (message.StartsWith("!выключитьии") && (username == "pdvrr" || username == "martellx" || e.ChatMessage.UserType == UserType.Moderator))
            {
                aiAllowed = false;
                client.SendMessage(channel, "ИИ отключен :(");
            }
            else if (message.StartsWith("!ssc") && (username == "pdvrr" || username == "martellx" || e.ChatMessage.UserType == UserType.Moderator))
            {
                int.TryParse(string.Join("", message.Where(char.IsDigit)), out var number);
                spamSymbolCount = number > 500 ? 500 : number;
                client.SendMessage(channel, spamSymbolCount + " ssc");
            }
            else if (message.StartsWith("!включитьии") && (username == "pdvrr" || username == "martellx" || e.ChatMessage.UserType == UserType.Moderator))
            {
                int.TryParse(string.Join("", message.Where(char.IsDigit)), out var number);
                AI_COMMAND = number;
                aiAllowed = true;
                client.SendMessage(channel, "ИИ включен :)");
            }
            else if (message.StartsWith("!разрешить спам") && (username == "pdvrr" || username == "martellx" || e.ChatMessage.UserType == UserType.Moderator))
            {
                int.TryParse(string.Join("", message.Where(char.IsDigit)), out var number);
                spamAllowed = true;
                SPAM_COMMAND = number;
                spamCommand = SPAM_COMMAND;
                client.SendMessage(channel, "Спам разрешен SadCat");
            }
            else if (message.StartsWith("!спам") && !message.ContainsAlias(Aliases.ProhibitedAliases) && spamCommand == 0 && spamAllowed && !bannedUsers.Contains(username))
            {
                spamCommand = SPAM_COMMAND;
                message = e.ChatMessage.Message.Replace("!спам ", "");
                SendSpamMessage(message, channel);
            }
            else if (message.StartsWith("!события") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                SendEventMessage(username, message, channel);
            }
            else if (message.StartsWith("!канобу") && spamCommand ==  0 && spamAllowed)
            {
                spamCommand = SPAM_COMMAND;
                message = message.Replace("!канобу ", "");
                SendKanobuMessage(username, message, channel);
            }
            else if (message.StartsWith("!длина") && spamCommand ==  0 && spamAllowed)
            {
                spamCommand = SPAM_COMMAND;
                username = SendAMessageTo(username, ref message);
                client.SendMessage(channel, $"@{username} {random.Next(0, 100)} см");
            }
            else if (message.StartsWith("!количество") && spamCommand ==  0 && spamAllowed)
            {
                spamCommand = SPAM_COMMAND;
                username = SendAMessageTo(username, ref message);
                message = message.Replace("!количество ", "");
                client.SendMessage(channel, $"@{username} {random.Next(10000)} {message}");
            }
            else if (message.StartsWith("!ban") && spamCommand ==  0 && spamAllowed)
            {
                spamCommand = SPAM_COMMAND;
                username = SendAMessageTo(username, ref message);
                client.SendMessage(channel, $"@{username} поздравляю, тебя забанили на {random.Next(100000)} секунд :)");
            }
            else if (message.StartsWith("!unban") && spamCommand ==  0 && spamAllowed)
            {
                spamCommand = SPAM_COMMAND;
                username = SendAMessageTo(username, ref message);
                client.SendMessage(channel, $"@{username} тебя разбанили. Можешь снова писать в чате :(");
            }
            else if (message.StartsWith("!github") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                client.SendMessage(channel, $"@{username} /PDVRR/HepegaTwitchBot");
            }
            else if (message.Contains("@hepega_bot") && aiCommand == 0 && aiAllowed)
            {
                if (message.ContainsAlias(Aliases.AnfisaAliases))
                    return;
                aiCommand = AI_COMMAND;
                message = message.Replace("@hepega_bot ", "");
                SendAnswerMessage(channel, message, username);
            }
            else if (message.StartsWith("!описание") && highCommand == 0)
            {
                highCommand = HIGH_COMMAND;
                message = message.Replace("!описание ", "").ToLower();
                SendItemMessage(message, channel, username);
            }
            else if (message.StartsWith("!hltb") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                message = message.Replace("!hltb ", "").ToLower();
                SendHltbMessage(username, message, channel);
            }
            else if (message.StartsWith("!gamefaq") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                message = message.Replace("!gamefaq ", "").ToLower();
                SendGamefaqMessage(username, message, channel);
            }
            else if (message.StartsWith("!коронавирус") && highCommand == 0)
            {
                highCommand = HIGH_COMMAND;
                message = message.Replace("!коронавирус ", "").ToLower();
                SendCoronavirusMessage(username, message, channel);
            }
            else if (message[0] != '@' && message[0] != '!' && !username.Contains("bot") && username != "streamelements" && !message.Contains("http") && !message.ToLower().Contains(_channel) && channel != "unclebjorn" && RepeatAllowed)
            {
                ProcessRandomMessage(channel, e.ChatMessage.Message);
            }
        }

        private void SendKanobuMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            int botIndex = random.Next(0, 3); // ka no bu
            int userIndex = -1;
            for (int i = 0; i < kanobu.Length; i++)
            {
                if (message.Contains(kanobu[i]))
                    userIndex = i;
            }

            bool win = false;
            bool draw = false;
            if (userIndex != -1)
            {
                string result = kanobuAnswer[botIndex];
                switch (botIndex)
                {
                    case 0:
                        if (userIndex == 1)
                        {
                            win = true;
                        }
                        break;
                    case 1:
                        if (userIndex == 2)
                        {
                            win = true;
                        }
                        break;
                    case 2:
                        if (userIndex == 0)
                        {
                            win = true;
                        }
                        break;
                }
                if (userIndex == botIndex)
                {
                    draw = true;
                }

                if (draw)
                {
                    result += ". Ничья. blushW";
                }

                if (win)
                {
                    result += ". Ты проиграл. Сосать + лежать EZ";
                }
                else if (win == false && !draw)
                {
                    result += ". Ты выиграл. SadCat";
                }
                client.SendMessage(channel, $"@{username} {result}");
            }
        }

        private void ProcessRandomMessage(string channel, string message)
        {
            string substring = string.Join(" ", message.ToLower().Split(' ').Distinct());
            substring = substring.ToLower();
            if (!messages.ContainsKey(substring))
            {
                messages.Add(substring, 1);
            }
            else
            {
                messages[substring] = (messages[substring] + 1);
            }

            if (messages[substring] > (channel == "uselessmouth" ? 6 : (random.Next(7, 12))))
            {
                client.SendMessage(channel, message);
                messages[substring] = 0;
            }
        }

        private async void SendCoronavirusMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            client.SendMessage(channel, $"@{username} " + await coronaParser.GetCoronaStatsByCountry(message));
        }

        private async void SendAnswerMessage(string channel, string message, string username)
        {
            client.SendMessage(channel, $"@{username} {await anfisa.GetResponse(message)}");
        }

        private void SendYaEbuMessage(string message, string channel)
        {
            StringBuilder builder = new StringBuilder(spamSymbolCount + 100);
            while (builder.Length <= spamSymbolCount)
            {
                builder.Append($"polarExtreme Я ЕБУ {message} ");
            }

            string result;
            if (builder.Length > spamSymbolCount)
            {
                result = builder.ToString().Substring(0, spamSymbolCount);
            }
            else
            {
                result = builder.ToString();
            }

            client.SendMessage(channel, $"{result}");
        }

        private void SendSpamMessage(string message, string channel)
        {
            StringBuilder builder = new StringBuilder(spamSymbolCount + 100);
            spamCount++;
            while (builder.Length <= spamSymbolCount)
            {
                builder.Append($"{message} ");
            }

            string result;
            if (builder.Length > spamSymbolCount)
            {
                result = builder.ToString().Substring(0, spamSymbolCount);
            }
            else
            {
                result = builder.ToString();
            }


            if (spamCount > 10)
            {
                client.SendMessage(channel, $"НЕ СПАМЬТЕ ПОЖАЛУСТО SadCat");
                spamCount = 0;
            }
            else
            {
                client.SendMessage(channel, $"{result}");
            }
        }

        private void SendItemMessage(string message, string channel, string username)
        {
            username = SendAMessageTo(username, ref message);
            if (items.Any(s => s.ToLower().Contains(message)))
            {
                string result = items.Where(s => s.ToLower().Substring(0,30).Contains(message)).OrderByDescending(s => s.IndexOf(message, StringComparison.Ordinal)).ToArray()[0];
                if (result.Length > 500)
                {
                    result = result.Substring(0, 470);
                    client.SendMessage(channel, $"{result} [Продолжение читай на сайте]");
                }
                else
                {
                    client.SendMessage(channel, $"{result}");
                }
            }
            else
            {
                client.SendMessage(channel, $"@{username} не найдено.");
            }
        }

        private async void SendHltbMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            string result;
            ParticipantInfo participantInfo = HasAParicipantAlias(message);
            string[] times;
            if (participantInfo != null)
            {
                times = await hltbParser.ParseGame(participantInfo.Game);
            }
            else
            {
                times = await hltbParser.ParseGame(message);
            }

            if (times != null)
            {
                result = $"Main story: {times[0].Replace("½", ".5")}. Main+Extra: {times[1].Replace("½", ".5")}";
            }
            else
            {
                result = "не удалось найти информацию об этой игре";
            }

            client.SendMessage(channel, $"@{username} {result}");
        }

        private async void SendGamefaqMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            string result;
            GamefaqStats stats;
            ParticipantInfo participantInfo = HasAParicipantAlias(message);
            if (participantInfo != null)
            {
                stats = await gamefaqParser.ParseGame(participantInfo.Game);
            }
            else
            {
                stats = await gamefaqParser.ParseGame(message);
            }

            if (stats != null)
            {
                result = $"Length: {stats.Time}. Completed: {stats.Completed}. Rating: {stats.Rating}";
            }
            else
            {
                result = "не удалось найти информацию об этой игре";
            }

            client.SendMessage(channel, $"@{username} {result}");
        }

        private void SendGameMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            string result;
            ParticipantInfo participantInfo = HasAParicipantAlias(message);
            if (participantInfo != null)
            {
                result = $"[{participantInfo.Section}] {participantInfo.Game}. Время прохождения: {participantInfo.HoursToComplete.Trim()}. Номинальное GGP: {participantInfo.NominalGgp} [{participantInfo.FinalGgp}]";
            }
            else
            {
                result = "участник не найден.";
            }
            client.SendMessage(channel, $"@{username} {result}");
        }

        private void SendEventMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            string result;
            ParticipantInfo participantInfo = HasAParicipantAlias(message);
            if (participantInfo != null)
            {
                result = participantInfo.Events;
            }
            else
            {
                result = "участник не найден.";
            }
            client.SendMessage(channel, $"@{username} {result}");
        }

        private ParticipantInfo HasAParicipantAlias(string message)
        {
            ParticipantInfo result;
            if (message.ContainsAlias(Aliases.MelAliases))
            {
                result = ParticipantsStats.ParticipantDictionary["mel"];
            }
            else if (message.ContainsAlias(Aliases.UzyaAliases))
            {
                result = ParticipantsStats.ParticipantDictionary["useless"];
            }
            else if (message.ContainsAlias(Aliases.LizonAliases))
            {
                result = ParticipantsStats.ParticipantDictionary["lizon"];
            }
            else if (message.ContainsAlias(Aliases.BjornAliases))
            {
                result = ParticipantsStats.ParticipantDictionary["bjorn"];
            }
            else if (message.ContainsAlias(Aliases.LasqaAliases))
            {
                result = ParticipantsStats.ParticipantDictionary["lasqa"];
            }
            else if (message.ContainsAlias(Aliases.FakerAliases))
            {
                result = ParticipantsStats.ParticipantDictionary["faker"];
            }
            else
            {
                result = null;
            }

            return result;
        }

        private string SendAMessageTo(string username, ref string message)
        {
            string nickname;
            if (message.Contains('@'))
            {
                int nickIndex = message.IndexOf('@');
                nickname = message.Substring(nickIndex + 1);
                message = message.Replace(" @" + nickname, "");
            }
            else
            {
                nickname = username;
            }
            return nickname;
        }
    }
}
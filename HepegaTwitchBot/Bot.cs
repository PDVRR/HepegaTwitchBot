using System;
using System.Collections.Generic;
using System.Linq;
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
        private int EASY_COMMAND = 3, MEDIUM_COMMAND = 5, HIGH_COMMAND = 7, SPAM_COMMAND = 1, AI_COMMAND = 4;
        TwitchClient client;
        private HpgDocParser hpgDoc;
        private List<string> items;
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
        private bool spamAllowed = false;
        private int spamSymbolCount = 390;
        private bool aiAllowed = false;
        private Dictionary<string, int> messages;
        private List<string> bannedUsers;

        public bool PrintLog { get; set; } = false;

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
            hpgDoc = new HpgDocParser();
            items = HpgItems.Items;
            hltbParser = new HltbParser();
            gamefaqParser = new GamefaqParser();
            coronaParser = new CoronavirusParser();
            messages = new Dictionary<string, int>();
            anfisa = new Anfisa();
            random = new Random();
            bannedUsers = new List<string>();
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
            string message = e.ChatMessage.Message;
            string channel = e.ChatMessage.Channel;
            string username = e.ChatMessage.Username;
            if (message.StartsWith("!команды") && easyCommand == 0)
            {
                username = SendAMessageTo(username, ref message);
                easyCommand = EASY_COMMAND;
                client.SendMessage(channel, $"@{username} Список доступных команд: !топхпг, !хпгигра [ник_участника], !события [ник_участника], !шмотка [название_шмотки_или_события], !hltb [название_игры], !gamefaq [название_игры], !коронавирус [название_страны_на_английском], !github");
            }
            else if (message.StartsWith("!топхпг") && mediumCommand == 0)
            {
                username = SendAMessageTo(username, ref message);
                mediumCommand = MEDIUM_COMMAND;
                client.SendMessage(channel, $"@{username} {hpgDoc.GetLeaderboard()}");
            }
            else if (message.StartsWith("!хпгигра") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                SendGameMessage(username, message, channel);
            }
            else if (message.StartsWith("!яебу") && !message.ContainsAlias(Aliases.ProhibitedAliases) && spamCommand == 0 && spamAllowed && !bannedUsers.Contains(username))
            {
                spamCommand = SPAM_COMMAND;
                message = message.Replace("!яебу", "");
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
                message = message.Replace("!спам ", "");
                SendSpamMessage(message, channel);
            }
            else if (message.StartsWith("!события") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                SendEventMessage(username, message, channel);
            }
            else if (message.StartsWith("!github") && mediumCommand == 0)
            {
                mediumCommand = MEDIUM_COMMAND;
                client.SendMessage(channel, $"@{username} /PDVRR/HepegaTwitchBot");
            }
            else if (message.Contains("@hepega_bot") && aiCommand == 0 && aiAllowed)
            {
                if(message.ContainsAlias(Aliases.AnfisaAliases))
                    return;
                aiCommand = AI_COMMAND;
                message = message.Replace("@hepega_bot ", "");
                SendAnswerMessage(channel, message, username);
            }
            else if (message.StartsWith("!шмотка") && highCommand == 0)
            {
                highCommand = HIGH_COMMAND;
                message = message.Replace("!шмотка ", "").ToLower();
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
            else if (message[0] != '@' &&  message[0] != '!' && !username.Contains("bot") && username != "streamelements" && !message.Contains("http") && !message.ToLower().Contains(_channel) && channel != "unclebjorn")
            {
                ProcessRandomMessage(channel, message);
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

            if (messages[substring] > (channel == "uselessmouth" ? 5 : (random.Next(7,10))))
            {
                client.SendMessage(channel, message);
                messages[substring] = 0;
            }
        }

        private async void SendCoronavirusMessage( string username, string message, string channel)
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
            string result = "";
            while (result.Length <= spamSymbolCount)
            {
                result += $"polarExtreme Я ЕБУ {message} ";
            }

            if (result.Length >= spamSymbolCount)
            {
                result = result.Substring(0, spamSymbolCount);
            }
            client.SendMessage(channel, $"{result}");
        }

        private void SendSpamMessage(string message, string channel)
        {
            spamCount++;
            string result = "";
            while (result.Length <= spamSymbolCount)
            {
                result += $"{message} ";
            }

            if (result.Length >= spamSymbolCount)
            {
                result = result.Substring(0, spamSymbolCount);
            }

            if (spamCount > 9)
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
            if (items.Any(s => s.ToLower().Contains(message.ToLower())))
            {
                
                string result = items.Where(s => s.ToLower().Contains(message.ToLower())).OrderByDescending(s => s.IndexOf(message, StringComparison.Ordinal)).ToArray()[0];
                if (result.Length > 500)
                {
                    
                    client.SendMessage(channel, $"@{username} слишком длинное описание. Читай в доке.");
                }
                else
                {
                    client.SendMessage(channel, $"@{username} {result}");
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
            string nickname = HasAParicipantAlias(message);
            if (nickname != "null")
            {
                ParticipantInfo participantInfo = hpgDoc.GetParticipantInfo(nickname);
                result = await hltbParser.ParseGame(participantInfo.Game);
            }
            else
            {
                result = await hltbParser.ParseGame(message);
            }

            client.SendMessage(channel, $"@{username} {result}");
        }

        private async void SendGamefaqMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            string result;
            string nickname = HasAParicipantAlias(message);
            if (nickname != "null")
            {
                ParticipantInfo participantInfo = hpgDoc.GetParticipantInfo(nickname);
                result = await gamefaqParser.ParseGame(participantInfo.Game);
            }
            else
            {
                result = await gamefaqParser.ParseGame(message);
            }

            client.SendMessage(channel, $"@{username} {result}");
        }

        private void SendGameMessage(string username, string message, string channel)
        {
            username = SendAMessageTo(username, ref message);
            string result;
            string nickname = HasAParicipantAlias(message);
            if (nickname != "null")
            {
                ParticipantInfo participantInfo = hpgDoc.GetParticipantInfo(nickname);
                result = $"[{participantInfo.Section}] {participantInfo.Game}. Номинальное GGP: {participantInfo.NominalGgp} [{hpgDoc.GetLastGameGgp(participantInfo)}]";
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
            string nickname = HasAParicipantAlias(message);
            if (nickname != "null")
            {
                ParticipantInfo participantInfo = hpgDoc.GetParticipantInfo(nickname);
                result = participantInfo.Events;
            }
            else
            {
                result = "участник не найден.";
            }
            client.SendMessage(channel, $"@{username} {result}");
        }

        private string HasAParicipantAlias(string message)
        {
            string result;
            if (message.ContainsAlias(Aliases.MelAliases))
            {
                result = "Melharucos";
            }
            else if (message.ContainsAlias(Aliases.UzyaAliases))
            {
                result = "UselessMouth";
            }
            else if (message.ContainsAlias(Aliases.LizonAliases))
            {
                result = "liz0n";
            }
            else if (message.ContainsAlias(Aliases.BjornAliases))
            {
                result = "UncleBjorn";
            }
            else if (message.ContainsAlias(Aliases.LasqaAliases))
            {
                result = "Lasqa";
            }
            else if (message.ContainsAlias(Aliases.FakerAliases))
            {
                result = "Mistafaker";
            }
            else
            {
                result = "null";
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
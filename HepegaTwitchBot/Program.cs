using System;
using System.Collections.Generic;
using System.Threading;

namespace HepegaTwitchBot
{
    class Program
    {
        private static readonly string[] DefaultChannels = { "liz0n", "uselessmouth", "unclebjorn", "mistafaker" };
        private static Dictionary<string, Bot> _bots;
        private static bool _exit = false;
        static void Main()
        {
            _bots = new Dictionary<string, Bot>();
            ConnectToChannel("defaultchannels");
            while (!_exit)
            {
                string[] commandLine = Console.ReadLine().Split(' ');
                string channel = "";
                if (commandLine.Length > 1)
                {
                    channel = commandLine[1].ToLower();
                }
                string command = commandLine[0].ToLower();
                switch (command)
                {
                    case "connect":
                        ConnectToChannel(channel);
                        break;
                    case "disconnect":
                        DisconnectFromChannel(channel);
                        break;
                    case "printlog":
                        PrintLog();
                        Console.Read();
                        PrintLog();
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    case "exit":
                        _exit = true;
                        break;
                }
            }
            Console.ReadLine();
        }

        private static void ConnectToChannel(string channel)
        {
            if (channel == "defaultchannels")
            {
                foreach (var defaultChannel in DefaultChannels)
                {
                    if (!_bots.ContainsKey(defaultChannel))
                    {
                        _bots.Add(defaultChannel, new Bot(defaultChannel));
                    }
                    ThreadPool.QueueUserWorkItem(o => { _bots[defaultChannel].Connect(); });
                }
            }
            else
            {
                if (!_bots.ContainsKey(channel))
                {
                    _bots.Add(channel, new Bot(channel));
                }
                ThreadPool.QueueUserWorkItem(o => { _bots[channel].Connect(); });
            }
        }

        private static void DisconnectFromChannel(string channel)
        {
            if (_bots.ContainsKey(channel))
            {
                _bots[channel].Disconnect();
            }
        }

        private static void PrintLog()
        {
            foreach (var bot in _bots)
            {
                bot.Value.PrintLog = !bot.Value.PrintLog;
            }
        }
    }
}

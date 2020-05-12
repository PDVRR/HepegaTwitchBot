﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace HepegaTwitchBot
{
    public class GamefaqParser
    {
        readonly HttpClient client;
        readonly string url = "https://gamefaqs.gamespot.com/search?game={game}";

        public GamefaqParser()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.129 Safari/537.36 Edg/81.0.416.68");
        }

        public async Task<string> ParseGame(string game)
        {
            string result = default;
            char[] arr = game.Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-')).ToArray();
            game = new string(arr);
            game = game.Replace("  ", " ");
            game = game.Replace(" ", "+");
            HttpResponseMessage response = await client.GetAsync(url.Replace("{game}", game));
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                var source = await response.Content.ReadAsStringAsync();

                HtmlParser domParser = new HtmlParser();
                IHtmlDocument document = await domParser.ParseDocumentAsync(source);
                List<IHtmlAnchorElement> items = document.QuerySelectorAll("a").OfType<IHtmlAnchorElement>()
                    .Where(item => item.ClassName != null && item.ClassName.Contains("log_search") && item.TextContent.Contains("PC")).ToList();
                if (items.Count != 0)
                {
                    string[] games = items.Select(item => item.PathName).ToArray();
                    result = await ParseTime(games[0]);
                }
                else
                {
                    result = "не удалось найти информацию об этой игре.";
                }
            }

            return result;
        }

        private async Task<string> ParseTime(string path)
        {
            string result = "";
            HttpResponseMessage response = await client.GetAsync("https://gamefaqs.gamespot.com/" + path);
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                var source = await response.Content.ReadAsStringAsync();

                HtmlParser domParser = new HtmlParser();
                IHtmlDocument document = await domParser.ParseDocumentAsync(source);
                List<IHtmlAnchorElement> items = document.QuerySelectorAll("a").OfType<IHtmlAnchorElement>()
                    .Where(item => item.PathName == (path + "/stats")).ToList();
                if (items.Count != 0)
                {
                    try
                    {
                        string completed =
                            items.Select(item => item).Where(item =>
                                    item.Href.Replace("about://", "") == (path + "/stats#play"))
                                .ToArray()[0].TextContent;
                        string rating =
                            items.Select(item => item).Where(item =>
                                    item.Href.Replace("about://", "") == (path + "/stats#rate"))
                                .ToArray()[0].TextContent;
                        string hours =
                            items.Select(item => item).Where(item =>
                                    item.Href.Replace("about://", "") == (path + "/stats#time"))
                                .ToArray()[0].TextContent;
                        result = $"Length: {hours}. Completed: {completed}. Rating: {rating}";
                    }
                    catch
                    {
                        result = "не удалось найти информацию об этой игре.";
                    }
                }
                else
                {
                    result = "не удалось найти информацию об этой игре.";
                }
            }

            return result;
        }
    }
}
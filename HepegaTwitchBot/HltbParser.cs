using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace HepegaTwitchBot
{
    public class HltbParser
    {
        readonly HttpClient client;
        readonly string url = "https://howlongtobeat.com/search_results?page=1";

        public HltbParser()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Friendly C# Bot");
            HttpContent httpContent = new StringContent("");
        }

        public async Task<string> ParseGame(string game)
        {
            string result = default;
            char[] arr = game.Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-')).ToArray();
            game = new string(arr);
            List<KeyValuePair<string, string>> formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("queryString", game),
                new KeyValuePair<string, string>("t", "games"),
                new KeyValuePair<string, string>("sorthead", "popular"),
                new KeyValuePair<string, string>("sortd", "Normal Order"),
                new KeyValuePair<string, string>("length_type", "main")
            };
            HttpContent content = new FormUrlEncodedContent(formData);
            HttpResponseMessage response = await client.PostAsync(url, content);
            string source = default;

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                source = await response.Content.ReadAsStringAsync();
                HtmlParser domParser = new HtmlParser();
                IHtmlDocument document = await domParser.ParseDocumentAsync(source);
                List<IElement> items = document.QuerySelectorAll("div")
                    .Where(item => item.ClassName != null && (item.ClassName.Contains("search_list_tidbit center time") || item.ClassName.Contains("search_list_tidbit_long center time"))).ToList();
                if (items.Count != 0)
                {
                    string[] times = items.Select(item => item.TextContent).ToArray();
                    result = $"Main story: {times[0].Replace("½",".5")}. Main+Extra: {times[1].Replace("½",".5")}";
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
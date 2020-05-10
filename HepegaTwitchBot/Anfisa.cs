using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HepegaTwitchBot
{
    public class Anfisa
    {
        private readonly string userid = "654321";

        public async Task<string> GetResponse(string request)
        {
            HttpClient client = new HttpClient();
            string json = "{\"ask\":\"" + request + "\",\"userid\":\"hepegabot\",\"key\":\"\"}";
            List<KeyValuePair<string, string>> fdDictionary = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("userid", userid),
                new KeyValuePair<string, string>("query", json)
            };
            HttpContent content = new FormUrlEncodedContent(fdDictionary);
            HttpResponseMessage response = await client.PostAsync("https://aiproject.ru/api/", content);
            dynamic jsonResponse = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (jsonResponse.aiml == null)
            {
                return "произошла непредвиденная ошибка.";
            }

            return jsonResponse.aiml;
        }
    }
}
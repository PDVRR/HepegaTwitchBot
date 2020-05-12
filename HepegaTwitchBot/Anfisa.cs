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
            Dictionary<string, string> fdDictionary = new Dictionary<string, string>
            {
                { "userid", userid },
                { "query", json }
            };
            HttpContent content = new FormUrlEncodedContent(fdDictionary);
            HttpResponseMessage response = await client.PostAsync("https://aiproject.ru/api/", content);
            dynamic jsonResponse = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            if (jsonResponse?.aiml == null)
            {
                return "произошла непредвиденная ошибка.";
            }

            return jsonResponse.aiml;
        }
    }
}
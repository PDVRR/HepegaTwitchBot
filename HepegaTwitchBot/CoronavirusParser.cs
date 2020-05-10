using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HepegaTwitchBot
{
    public class CoronavirusParser
    {
        public async Task<string> GetCoronaStatsByCountry(string country)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponseMessage =
                await httpClient.GetAsync(
                    $"https://corona.lmao.ninja/v2/countries/{country}?today=true&strict=true&query");
            dynamic jsonResponse = JsonConvert.DeserializeObject(await httpResponseMessage.Content.ReadAsStringAsync());
            if (jsonResponse.message == null)
            {
                return $"[+{jsonResponse.todayCases} за сегодня] Подтверждено: {jsonResponse.cases}. Выздоровевших: {jsonResponse.recovered}. Смертей: {jsonResponse.deaths}. Заражено в данный момент: {jsonResponse.active}.";
            }

            return $"{jsonResponse.message}";
        }
    }
}
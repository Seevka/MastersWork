using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace MastersWork.Helpers
{
    public class NewtonsoftJsonUpdate : Update
    {
        public static async ValueTask<NewtonsoftJsonUpdate?> BindAsync(HttpContext context)
        {
            using var streamReader = new StreamReader(context.Request.Body);
            var updateJsonString = await streamReader.ReadToEndAsync();

            return JsonConvert.DeserializeObject<NewtonsoftJsonUpdate>(updateJsonString);
        }
    }
}

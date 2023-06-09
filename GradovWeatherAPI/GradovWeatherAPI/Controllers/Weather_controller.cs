using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Telegram.Bot;

namespace nast.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Weather_controller : ControllerBase
    {
        [HttpPost("send_weather_to_telegram")]
        public async Task<ActionResult> PostWeather(long id, string city)
        {
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={constants.apikey}");
            string result = await response.Content.ReadAsStringAsync();

            if (result != "{\"cod\":\"404\",\"message\":\"city not found\"}")
            {

                WeatherClass.Root city_weather = JsonConvert.DeserializeObject<WeatherClass.Root>(result);
                double temperature = Convert.ToDouble(city_weather.main.temp) - 273.15;
                double feelsLike = Convert.ToDouble(city_weather.main.feels_like) - 273.15;
                double minTemperature = Convert.ToDouble(city_weather.main.temp_min) - 273.15;
                double maxTemperature = Convert.ToDouble(city_weather.main.temp_max) - 273.15;
                int pressure = city_weather.main.pressure;
                int humidity = city_weather.main.humidity;
                string weatherCondition = city_weather.weather[0].main;
                string weatherDescription = city_weather.weather[0].description;

                string message = $"Погода у місті {city}:\n" +
                    $"Температура: {Math.Round(temperature, 2)} °C\n" +
                    $"Відчувається як: {Math.Round(feelsLike, 2)} °C\n" +
                    $"Мінімальна температура: {Math.Round(minTemperature, 2)} °C\n" +
                    $"Максимальна температура: {Math.Round(maxTemperature, 2)} °C\n" +
                    $"Тиск: {pressure} гПа\n" +
                    $"Вологість: {humidity}%\n" +
                    $"Погодні умови: {weatherCondition}\n" +
                    $"Опис: {weatherDescription}";

                await bot.SendTextMessageAsync(id, message);

                return Ok();
            }
            else
            {
                await bot.SendTextMessageAsync(id, "Такого міста не існує");
                return BadRequest();
            }
        }


        [HttpPut("put_city_to_list")]
        public async Task<ActionResult> PutCityToList(long id, string city)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);
            var response = await httpClient.GetAsync($"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={constants.apikey}");
            var result = await response.Content.ReadAsStringAsync();

            if (result != "{\"cod\":\"404\",\"message\":\"city not found\"}")
            {
                var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
                var document = constants.collection.Find(filter).FirstOrDefault();

                var my_cities = document["saved_city"].AsBsonArray;

                bool hasDuplicate = my_cities.AsQueryable()
        .Any(a => a == city);


                if (hasDuplicate)
                {
                    await bot.SendTextMessageAsync(id, "Це місто вже є у вашому списку.");
                }
                else
                {
                    BsonValue bsonValue = BsonValue.Create(city);

                    my_cities.Add(bsonValue);
                    var update = Builders<BsonDocument>.Update.Set("saved_city", my_cities);
                    constants.collection.UpdateOne(filter, update);
                    await bot.SendTextMessageAsync(id, "Місто успішно додано до вашого списку");
                }
                return Ok();
            }
            else
            {
                await bot.SendTextMessageAsync(id, "Такого міста не існує");
                return BadRequest();
            }
        }
        [HttpPost("post_my_cities_list")]
        public async Task<ActionResult> PostMyCitiesList(long id)
        {
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();

            var cities = document["saved_city"].AsBsonArray;
            if (cities.Count == 0)
            {
                await bot.SendTextMessageAsync(id, "Ваш список порожній");
            }
            else
            {
                for (int i = 0; i < cities.Count; i++)
                {
                    await bot.SendTextMessageAsync(id, $"Номер міста у вашому списку: {i + 1}\nНазва міста: {cities[i]}");
                }
            }
            //await bot.SendTextMessageAsync(id, Convert.ToString(myAsteroids));
            return Ok();
        }
        [HttpDelete("delete_city_from_list")]
        public async Task<ActionResult> DeleteCityFromList(long id, int cityIndex)
        {
            cityIndex--;
            HttpClient httpClient = new HttpClient();
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            var my_cities = document["saved_city"].AsBsonArray;

            if (cityIndex >= 0 && cityIndex < my_cities.Count)
            {
                string city = my_cities[cityIndex].AsString;
                my_cities.RemoveAt(cityIndex);
                var update = Builders<BsonDocument>.Update.Set("saved_city", my_cities);
                constants.collection.UpdateOne(filter, update);
                await bot.SendTextMessageAsync(id, $"Місто {city} успішно видалено з вашого списку");
                return Ok();
            }
            else
            {
                await bot.SendTextMessageAsync(id, "Неправильний номер");
                return BadRequest();
            }
        }


        [HttpPut("bot_is_waiting_for_city")]
        public ActionResult<string> BotIsWaitingForCity(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_city", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_city")]
        public ActionResult<bool> BotIsWaitingForCity(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_city", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPut("bot_is_waiting_for_city_to_add")]
        public ActionResult<string> BotIsWaitingForCityToAdd(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_city_to_add", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_city_to_add")]
        public ActionResult<bool> BotIsWaitingForCityToAdd(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_city_to_add", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPut("bot_is_waiting_for_city_to_remove")]
        public ActionResult<string> BotIsWaitingForCityToRemove(long id, bool b)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var update = Builders<BsonDocument>.Update.Set("bot_is_waiting_for_city_to_remove", b);
            var result = constants.collection.UpdateOne(filter, update);
            return Ok();
        }

        [HttpGet("bot_is_waiting_for_city_to_remove")]
        public ActionResult<bool> BotIsWaitingForCityToRemove(long id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();
            if (document != null && document.TryGetValue("bot_is_waiting_for_city_to_remove", out BsonValue value))
            {
                return value.AsBoolean;
            }

            return NotFound();
        }
        [HttpPost("daily_post")]
        public async Task<ActionResult> DailyPost(long id)
        {
            ITelegramBotClient bot = new TelegramBotClient(constants.botId);
            HttpClient httpClient = new HttpClient();

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", id);
            var document = constants.collection.Find(filter).FirstOrDefault();

            var cities = document["saved_city"].AsBsonArray;
            if (cities.Count != 0)
            {
                await bot.SendTextMessageAsync(id, "Ось погода ваших обраних міст на сьогодні:");
                for (int i = 0; i < cities.Count; i++)
                {
                    await httpClient.PostAsync($"https://{constants.host}/Weather_/send_weather_to_telegram?id={id}&city={cities[i]}", null);
                }
            }
            return Ok();
        }
    }
}
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

class Program
{
    static ITelegramBotClient bot = new TelegramBotClient(constants.botId);
    static HttpClient httpClient = new HttpClient();
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
        {
            var message = update.Message;
            User user = message.From;
            string user_firstname = user.FirstName;
            long user_id = user.Id;

            var saved_city = new List<string>();

            var document = new BsonDocument
                    {
                        { "user_id", user_id},
                        { "user_firstname", user_firstname },
                {"bot_is_waiting_for_city", false },
                {"bot_is_waiting_for_city_to_add", false },
                {"bot_is_waiting_for_city_to_remove", false },
                  {"saved_city", new BsonArray(saved_city.Select(t => t.ToBsonDocument())) }
                };

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var exists = constants.collection.Find(filter).Any();

            if (!exists)
            {
                constants.collection.InsertOne(document);
            }

            var resp = await httpClient.GetAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city?id={user_id}");
            var res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_city = Convert.ToBoolean(res);

            resp = await httpClient.GetAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city_to_add?id={user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_city_to_add = Convert.ToBoolean(res);

            resp = await httpClient.GetAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city_to_remove?id={user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_city_to_remove = Convert.ToBoolean(res);

            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(user_id, "Привіт!\nЯ допоможу вам отримати погоду у вашому місті.\nОсь основні команди:\n/weather - подивитися погоду у місті за назвою\n/my_cities - подивитися список обраних міст\n/add_city - додати місто у список обраних\n/delete_city - видалити місто зі списка обраних");
                return;
            }
            if (message.Text.ToLower() == "/weather")
            {
                await botClient.SendTextMessageAsync(user_id, "Введіть назву міста, погоду якого ви хочете дізнатися.");
                await httpClient.PutAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city?id={user_id}&b=true", null);
                return;
            }
            if (message.Text.ToLower() == "/my_cities")
            {
                await botClient.SendTextMessageAsync(user_id, "Ось міста, які ви додали у свій список:");
                await httpClient.PostAsync($"https://{constants.host}/Weather_/post_my_cities_list?id={user_id}", null);
                return;
            }
            if (message.Text.ToLower() == "/add_city")
            {
                await botClient.SendTextMessageAsync(user_id, "Введіть назву міста, яке ви хочете додати до свого списку.");
                await httpClient.PutAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city_to_add?id={user_id}&b=true", null);
                return;
            }
            if (message.Text.ToLower() == "/delete_city")
            {
                await botClient.SendTextMessageAsync(user_id, "Введіть номер міста, яке ви хочете видалити зі свого списку.");
                await httpClient.PutAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city_to_remove?id={user_id}&b=true", null);
                return;
            }


            if (bot_is_waiting_for_city)
            {
                string city = message.Text;
                await httpClient.PostAsync($"https://{constants.host}/Weather_/send_weather_to_telegram?id={user_id}&city={city}", null);
                await httpClient.PutAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city?id={user_id}&b=false", null);
                return;
            }
            if (bot_is_waiting_for_city_to_add)
            {
                string city = message.Text;
                await httpClient.PutAsync($"https://{constants.host}/Weather_/put_city_to_list?id={user_id}&city={city}", null);
                await httpClient.PutAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city_to_add?id={user_id}&b=false", null);
                return;
            }
            if (bot_is_waiting_for_city_to_remove)
            {
                string number_city = message.Text;
                await httpClient.DeleteAsync($"https://{constants.host}/Weather_/delete_city_from_list?id={user_id}&cityIndex={number_city}");
                await httpClient.PutAsync($"https://{constants.host}/Weather_/bot_is_waiting_for_city_to_remove?id={user_id}&b=false", null);
                return;
            }
            await botClient.SendTextMessageAsync(user_id, "Я не розумію, що ти хочеш");
            return;
        }
        else
        {
            if (update.Message != null && update.Message.From != null)
            {
                long user_id = update.Message.From.Id;
                await botClient.SendTextMessageAsync(user_id, "Я розрахований лише на текстові повідомлення");
            }
        }
    }
    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }


    public static async Task DailyUpdate()
    {
        ITelegramBotClient bott = new TelegramBotClient(constants.botId);
        while (true)
        {
            if (DateTime.UtcNow.Hour == 7 && DateTime.UtcNow.Minute == 0)
            {
                var filter = Builders<BsonDocument>.Filter.Empty;
                var documents = constants.collection.Find(filter).ToList();

                foreach (var document in documents)
                {
                    long user_id = Convert.ToInt64(document["user_id"]);
                    await httpClient.PostAsync($"https://{constants.host}/Weather_/daily_post?id={user_id}", null);
                }
                Thread.Sleep(90000);
            }
        }
    }






    static void Main(string[] args)
    {

        Task.Run(async () => await DailyUpdate());

        Console.WriteLine("Запущен бот" + bot.GetMeAsync().Result.FirstName);

        constants.mongoClient = new MongoClient("mongodb+srv://hradovdenys:polkipolki4@weather.j8i7l5t.mongodb.net/");
        constants.database = constants.mongoClient.GetDatabase("weather1");
        constants.collection = constants.database.GetCollection<BsonDocument>("collection1");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { },
        };
        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
        //Console.ReadLine();
    }
}
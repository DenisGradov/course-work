using MongoDB.Bson;
using MongoDB.Driver;

public class constants
{
    public static string botId = "6277172749:AAEGl8LJRy83kIgVxL3gtW2zvGnUk8L1XP8";
    public static string host = "gradovweatherapi20230528191122.azurewebsites.net";
    public static MongoClient mongoClient;
    public static IMongoDatabase database;
    public static IMongoCollection<BsonDocument> collection;
    public static string apikey = "645ba868c66aa4fc529b4df70645d0b7";
}
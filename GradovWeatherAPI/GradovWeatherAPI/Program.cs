using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;

constants.mongoClient = new MongoClient("mongodb+srv://hradovdenys:polkipolki4@weather.j8i7l5t.mongodb.net/");
constants.database = constants.mongoClient.GetDatabase("weather1");
constants.collection = constants.database.GetCollection<BsonDocument>("collection1");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// äîáàâëåíèå Swagger
builder.Services.AddSwaggerGen(options =>
{
    // çàäàíèå ïàðàìåòðîâ Swagger
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });
});

var app = builder.Build();

// íàñòðîéêà Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

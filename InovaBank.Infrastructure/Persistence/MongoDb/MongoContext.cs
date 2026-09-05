using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace InovaBank.Infrastructure.Persistence.MongoDb;

public sealed class MongoContext
{
    public IMongoClient Client { get; }
    private readonly IMongoDatabase _database;

    public MongoContext(IConfiguration configuration)
    {
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
        }

        var connectionString = configuration.GetConnectionString("MongoDb");
        Client = new MongoClient(connectionString);
        _database = Client.GetDatabase("InovaBank_ReadModel");
    }

    public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);
}

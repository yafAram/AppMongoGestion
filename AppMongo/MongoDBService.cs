using MongoDB.Driver;

namespace AppMongo
{
    public class MongoDBService
    {
        private readonly IMongoClient _client;
        public MongoDBService(IConfiguration config)
        {
            _client = new MongoClient(config.GetConnectionString("MongoDB"));
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            await _client.GetDatabase(databaseName).CreateCollectionAsync("default");
        }
    }

}

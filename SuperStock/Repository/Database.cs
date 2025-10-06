using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SuperStock.Repository;

public class Database
{
    public readonly IMongoCollection<Ticket> TicketCollection;

    public Database(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        TicketCollection = mongoDatabase.GetCollection<Ticket>(
            databaseSettings.Value.TicketCollectionName);
    }

    public async Task Init()
    {
        const string id = "dcbc9f373e7c96cae045a587";

        await TicketCollection.UpdateOneAsync(
            filter: Builders<Ticket>.Filter.Eq(x => x.Id, id),
            update: Builders<Ticket>.Update.Set(x => x.Stock, 3000),
            options: new UpdateOptions { IsUpsert = true });
    }
}

public class DatabaseInitializer(Database database) : IHostedService
{
    private readonly Database _database = database;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _database.Init();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
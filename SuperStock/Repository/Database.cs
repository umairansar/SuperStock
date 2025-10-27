using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SuperStock.Repository;

public class Database
{
    public readonly IMongoCollection<Ticket> TicketCollection;
    public readonly IMongoCollection<Product> ProductCollection;

    public Database(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);
        
        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        TicketCollection = mongoDatabase.GetCollection<Ticket>(
            databaseSettings.Value.TicketCollectionName);

        ProductCollection = mongoDatabase.GetCollection<Product>(
            databaseSettings.Value.ProductCollectionName);
    }

    public async Task Init()
    {
        const string ticketId = "dcbc9f373e7c96cae045a587";

        await TicketCollection.UpdateOneAsync(
            filter: Builders<Ticket>.Filter.Eq(x => x.Id, ticketId),
            update: Builders<Ticket>.Update.Set(x => x.Stock, 5000),
            options: new UpdateOptions { IsUpsert = true });
        
        string[] productIds = ["dcbc9f373e7c96cae045a589", "dcbc9f373e7c96cae045a590", "dcbc9f373e7c96cae045a591"];

        await ProductCollection.UpdateOneAsync(
            filter: Builders<Product>.Filter.Eq(x => x.Id, productIds[0]),
            update: Builders<Product>.Update.Set(x => x.Stock, 5000),
            options: new UpdateOptions { IsUpsert = true });
        
        await ProductCollection.UpdateOneAsync(
            filter: Builders<Product>.Filter.Eq(x => x.Id, productIds[1]),
            update: Builders<Product>.Update.Set(x => x.Stock, 5000),
            options: new UpdateOptions { IsUpsert = true });
        
        await ProductCollection.UpdateOneAsync(
            filter: Builders<Product>.Filter.Eq(x => x.Id, productIds[2]),
            update: Builders<Product>.Update.Set(x => x.Stock, 5000),
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
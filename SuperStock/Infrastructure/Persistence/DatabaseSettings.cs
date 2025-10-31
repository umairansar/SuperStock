namespace SuperStock.Infrastructure.Persistence;

//https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mongo-app?view=aspnetcore-9.0&tabs=visual-studio
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string TicketCollectionName { get; set; } = null!;
    
    public string ProductCollectionName { get; set; } = null!;
}
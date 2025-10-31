using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SuperStock.Domain;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public int Stock { get; set; }
}
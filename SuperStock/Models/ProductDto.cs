using SuperStock.Repository;

namespace SuperStock.Models;

public record ProductDto(string Id, int RemainingStock, int SoldStock);
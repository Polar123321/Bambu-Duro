namespace ConsoleApp4.Services.Models;

public sealed record InventoryItemDto(
    string Name,
    string Description,
    int Quantity,
    int SellPrice);

namespace ConsoleApp4.Services.Models;

public sealed record ShopItemDto(
    string Name,
    string Description,
    int BuyPrice,
    int SellPrice,
    bool IsConsumable);

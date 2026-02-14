namespace ConsoleApp4.Services.Models;

public sealed record InventoryResult(
    int TotalItems,
    IReadOnlyList<InventoryItemDto> Items);

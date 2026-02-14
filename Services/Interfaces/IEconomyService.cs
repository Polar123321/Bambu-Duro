using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IEconomyService
{
    Task<int> GetBalanceAsync(ulong userId, string username);
    Task<EconomyResult> WorkAsync(ulong userId, string username);
    Task<EconomyResult> CrimeAsync(ulong userId, string username);
    Task<EconomyResult> DailyAsync(ulong userId, string username);
    Task<EconomyResult> HuntAsync(ulong userId, string username);
    Task<ShopResult> BuyAsync(ulong userId, string username, string itemName, int quantity);
    Task<ShopResult> SellAsync(ulong userId, string username, string itemName, int quantity);
    Task<InventoryResult> GetInventoryAsync(ulong userId, string username, int skip, int take);
    Task<ShopResult> UseAsync(ulong userId, string username, string itemName, int quantity);
    Task<IReadOnlyList<ShopItemDto>> GetShopAsync(int skip, int take);
    Task<SpendResult> TrySpendAsync(ulong userId, string username, int amount, string reason);
}

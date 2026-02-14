using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Models.Enums;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class EconomyService : IEconomyService
{
    private readonly BotDbContext _db;
    private readonly EconomyConfiguration _config;

    public EconomyService(BotDbContext db, IOptions<EconomyConfiguration> config)
    {
        _db = db;
        _config = config.Value;
    }

    public async Task<int> GetBalanceAsync(ulong userId, string username)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        return user.Coins;
    }

    public async Task<EconomyResult> WorkAsync(ulong userId, string username)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var coins = Random.Shared.Next(_config.WorkMinReward, _config.WorkMaxReward + 1);
        var exp = Random.Shared.Next(_config.ExpMinWork, _config.ExpMaxWork + 1);

        user.Coins += coins;
        ApplyExperience(user, exp);

        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "work", coins, true, null);

        return new EconomyResult(true,
            $"Trabalho concluido! Voce ganhou {coins} moedas e {exp} EXP.",
            coins,
            exp,
            user.Coins,
            user.Level,
            user.Experience);
    }

    public async Task<EconomyResult> CrimeAsync(ulong userId, string username)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var roll = Random.Shared.Next(0, 100);
        if (roll < _config.CrimeSuccessChancePercent)
        {
            var coins = Random.Shared.Next(_config.CrimeMinReward, _config.CrimeMaxReward + 1);
            var exp = Random.Shared.Next(_config.ExpMinCrime, _config.ExpMaxCrime + 1);

            user.Coins += coins;
            ApplyExperience(user, exp);

            await _db.SaveChangesAsync();
            await LogTransactionAsync(user.Id, "crime", coins, true, null);

            return new EconomyResult(true,
                $"Crime bem sucedido. Voce ganhou {coins} moedas e {exp} EXP.",
                coins,
                exp,
                user.Coins,
                user.Level,
                user.Experience);
        }

        var fine = Random.Shared.Next(_config.CrimeMinFine, _config.CrimeMaxFine + 1);
        user.Coins = Math.Max(0, user.Coins - fine);

        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "crime", -fine, false, "falha");

        return new EconomyResult(false,
            $"Voce foi pego e perdeu {fine} moedas.",
            -fine,
            0,
            user.Coins,
            user.Level,
            user.Experience);
    }

    public async Task<EconomyResult> DailyAsync(ulong userId, string username)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        user.Coins += _config.DailyReward;

        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "daily", _config.DailyReward, true, null);

        return new EconomyResult(true,
            $"Bonus diario recebido: {_config.DailyReward} moedas.",
            _config.DailyReward,
            0,
            user.Coins,
            user.Level,
            user.Experience);
    }

    public async Task<EconomyResult> HuntAsync(ulong userId, string username)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var exp = Random.Shared.Next(_config.ExpMinHunt, _config.ExpMaxHunt + 1);
        var lootChance = Random.Shared.Next(0, 100);

        Item? loot = null;
        if (lootChance > 70)
        {
            loot = await _db.Items.OrderBy(i => Guid.NewGuid()).FirstOrDefaultAsync();
            if (loot != null)
            {
                await AddItemAsync(user.Id, loot.Id, 1);
            }
        }

        ApplyExperience(user, exp);
        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "hunt", 0, true, loot?.Name);

        var lootMessage = loot == null ? "Nenhum item encontrado." : $"Voce encontrou: {loot.Name}.";
        return new EconomyResult(true,
            $"Cacada concluida! Voce ganhou {exp} EXP. {lootMessage}",
            0,
            exp,
            user.Coins,
            user.Level,
            user.Experience);
    }

    public async Task<ShopResult> BuyAsync(ulong userId, string username, string itemName, int quantity)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var item = await FindItemAsync(itemName);
        if (item == null)
        {
            return new ShopResult(false, "Item nao encontrado.", user.Coins);
        }

        var safeQuantity = Math.Clamp(quantity, 1, 99);
        var totalCost = item.BuyPrice * safeQuantity;
        if (user.Coins < totalCost)
        {
            return new ShopResult(false, "Saldo insuficiente.", user.Coins);
        }

        user.Coins -= totalCost;
        await AddItemAsync(user.Id, item.Id, safeQuantity);
        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "buy", -totalCost, true, item.Name);

        return new ShopResult(true, $"Comprado: {item.Name} x{safeQuantity}.", user.Coins);
    }

    public async Task<ShopResult> SellAsync(ulong userId, string username, string itemName, int quantity)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var item = await FindItemAsync(itemName);
        if (item == null)
        {
            return new ShopResult(false, "Item nao encontrado.", user.Coins);
        }

        var safeQuantity = Math.Clamp(quantity, 1, 99);
        var userItem = await _db.UserItems.FirstOrDefaultAsync(ui => ui.UserId == user.Id && ui.ItemId == item.Id);
        if (userItem == null || userItem.Quantity < safeQuantity)
        {
            return new ShopResult(false, "Voce nao possui quantidade suficiente.", user.Coins);
        }

        userItem.Quantity -= safeQuantity;
        if (userItem.Quantity <= 0)
        {
            _db.UserItems.Remove(userItem);
        }

        var totalGain = item.SellPrice * safeQuantity;
        user.Coins += totalGain;
        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "sell", totalGain, true, item.Name);

        return new ShopResult(true, $"Vendido: {item.Name} x{safeQuantity}.", user.Coins);
    }

    public async Task<InventoryResult> GetInventoryAsync(ulong userId, string username, int skip, int take)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var items = await _db.UserItems
            .Where(ui => ui.UserId == user.Id)
            .OrderByDescending(ui => ui.Quantity)
            .Skip(skip)
            .Take(take)
            .Join(_db.Items, ui => ui.ItemId, i => i.Id,
                (ui, i) => new InventoryItemDto(i.Name, i.Description, ui.Quantity, i.SellPrice))
            .ToListAsync();

        var total = await _db.UserItems.CountAsync(ui => ui.UserId == user.Id);
        return new InventoryResult(total, items);
    }

    public async Task<ShopResult> UseAsync(ulong userId, string username, string itemName, int quantity)
    {
        var user = await GetOrCreateUserAsync(userId, username);
        var item = await FindItemAsync(itemName);
        if (item == null)
        {
            return new ShopResult(false, "Item nao encontrado.", user.Coins);
        }

        if (!item.IsConsumable)
        {
            return new ShopResult(false, "Este item nao pode ser usado.", user.Coins);
        }

        var safeQuantity = Math.Clamp(quantity, 1, 10);
        var userItem = await _db.UserItems.FirstOrDefaultAsync(ui => ui.UserId == user.Id && ui.ItemId == item.Id);
        if (userItem == null || userItem.Quantity < safeQuantity)
        {
            return new ShopResult(false, "Voce nao possui quantidade suficiente.", user.Coins);
        }

        userItem.Quantity -= safeQuantity;
        if (userItem.Quantity <= 0)
        {
            _db.UserItems.Remove(userItem);
        }

        var totalEffect = item.EffectValue * safeQuantity;
        if (item.EffectType == ItemEffectType.AddCoins)
        {
            user.Coins += totalEffect;
        }
        else if (item.EffectType == ItemEffectType.AddExperience)
        {
            ApplyExperience(user, totalEffect);
        }

        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, "use", totalEffect, true, item.Name);

        return new ShopResult(true, $"Item usado: {item.Name} x{safeQuantity}.", user.Coins);
    }

    public async Task<IReadOnlyList<ShopItemDto>> GetShopAsync(int skip, int take)
    {
        return await _db.Items
            .OrderBy(i => i.BuyPrice)
            .Skip(skip)
            .Take(take)
            .Select(i => new ShopItemDto(i.Name, i.Description, i.BuyPrice, i.SellPrice, i.IsConsumable))
            .ToListAsync();
    }

    public async Task<SpendResult> TrySpendAsync(ulong userId, string username, int amount, string reason)
    {
        var safeAmount = Math.Max(1, amount);
        var user = await GetOrCreateUserAsync(userId, username);
        if (user.Coins < safeAmount)
        {
            return new SpendResult(false, "Saldo insuficiente.", safeAmount, user.Coins);
        }

        user.Coins -= safeAmount;
        await _db.SaveChangesAsync();
        await LogTransactionAsync(user.Id, reason, -safeAmount, true, null);
        return new SpendResult(true, "Pagamento realizado.", safeAmount, user.Coins);
    }

    private async Task<User> GetOrCreateUserAsync(ulong userId, string username)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.DiscordUserId == userId);
        if (existing != null)
        {
            existing.Username = username;
            await _db.SaveChangesAsync();
            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            DiscordUserId = userId,
            Username = username,
            Coins = 0,
            Level = 1,
            Experience = 0
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<Item?> FindItemAsync(string itemName)
    {
        var normalized = itemName.Trim().ToLowerInvariant();
        return await _db.Items.FirstOrDefaultAsync(i => i.Name.ToLower() == normalized);
    }

    private async Task AddItemAsync(Guid userId, Guid itemId, int quantity)
    {
        var userItem = await _db.UserItems.FirstOrDefaultAsync(ui => ui.UserId == userId && ui.ItemId == itemId);
        if (userItem == null)
        {
            _db.UserItems.Add(new UserItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ItemId = itemId,
                Quantity = quantity
            });
            return;
        }

        userItem.Quantity += quantity;
    }

    private void ApplyExperience(User user, int expGain)
    {
        user.Experience += expGain;
        while (user.Experience >= GetRequiredExp(user.Level))
        {
            user.Experience -= GetRequiredExp(user.Level);
            user.Level += 1;
        }
    }

    private static int GetRequiredExp(int level)
    {
        return 100 + (level * 50);
    }

    private async Task LogTransactionAsync(Guid userId, string type, int amount, bool success, string? notes)
    {
        _db.EconomyTransactions.Add(new EconomyTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Amount = amount,
            Success = success,
            Notes = notes
        });
        await _db.SaveChangesAsync();
    }
}

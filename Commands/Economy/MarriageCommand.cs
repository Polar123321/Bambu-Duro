using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Economy;

public sealed class MarriageCommand : CommandBase
{
    private const int MarriageCost = 550;
    private readonly IEconomyService _economy;
    private readonly IMarriageStore _marriageStore;

    public MarriageCommand(
        IEconomyService economy,
        IMarriageStore marriageStore,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _economy = economy;
        _marriageStore = marriageStore;
    }

    [Command("casar")]
    [Alias("casamento", "marry")]
    [Summary("Casa com outro usuario (custo: 550 moedas).")]
    public async Task MarryAsync([Remainder] string? targetInput = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var target = await ResolveUserFromInputAsync(targetInput);
        if (target == null)
        {
            await ReplyAsync("Use: casar @usuario");
            return;
        }

        if (target.Id == Context.User.Id)
        {
            await ReplyAsync("Voce nao pode casar consigo mesmo.");
            return;
        }

        if (target.IsBot)
        {
            await ReplyAsync("Voce nao pode casar com bots.");
            return;
        }

        var records = (await _marriageStore.GetAsync(Context.Guild.Id)).ToList();
        if (IsMarried(records, Context.User.Id))
        {
            await ReplyAsync("Voce ja esta casado(a).");
            return;
        }

        if (IsMarried(records, target.Id))
        {
            await ReplyAsync($"{target.Mention} ja esta casado(a).");
            return;
        }

        var spend = await _economy.TrySpendAsync(Context.User.Id, Context.User.Username, MarriageCost, "marriage");
        if (!spend.Success)
        {
            await ReplyAsync($"Saldo insuficiente. Voce precisa de {MarriageCost} moedas e possui {spend.NewBalance}.");
            return;
        }

        var id1 = Math.Min(Context.User.Id, target.Id);
        var id2 = Math.Max(Context.User.Id, target.Id);
        records.Add(new MarriageRecord(id1, id2, DateTime.UtcNow));
        await _marriageStore.SaveAsync(Context.Guild.Id, records);

        var embed = EmbedHelper.CreateInfo("💍 Casamento!", "Parabens pelo casamento!")
            .AddField("Noivos", $"{Context.User.Mention} + {target.Mention}", false)
            .AddField("Custo", $"{MarriageCost} moedas", true)
            .AddField("Saldo Atual", $"{spend.NewBalance} moedas", true);

        await ReplyMajesticAsync(embed);
    }

    [Command("divorcio")]
    [Alias("divorce", "separar")]
    [Summary("Desfaz o casamento atual.")]
    public async Task DivorceAsync()
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var records = (await _marriageStore.GetAsync(Context.Guild.Id)).ToList();
        var record = records.FirstOrDefault(r => r.UserId1 == Context.User.Id || r.UserId2 == Context.User.Id);
        if (record == null)
        {
            await ReplyAsync("Voce nao esta casado(a).");
            return;
        }

        records.Remove(record);
        await _marriageStore.SaveAsync(Context.Guild.Id, records);

        var spouseId = record.UserId1 == Context.User.Id ? record.UserId2 : record.UserId1;
        var spouse = Context.Guild.GetUser(spouseId);
        var spouseText = spouse?.Mention ?? $"Usuario {spouseId}";

        var embed = EmbedHelper.CreateInfo("💔 Divorcio", "O casamento foi encerrado.")
            .AddField("Ex-casal", $"{Context.User.Mention} e {spouseText}", false);

        await ReplyMajesticAsync(embed);
    }

    private static bool IsMarried(IEnumerable<MarriageRecord> records, ulong userId)
    {
        return records.Any(r => r.UserId1 == userId || r.UserId2 == userId);
    }

    private async Task<IUser?> ResolveUserFromInputAsync(string? input)
    {
        if (Context.Guild == null || string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var normalizedInput = input.Trim();
        var lookupInput = NormalizeLookupInput(normalizedInput);

        if (TryParseUserId(normalizedInput, out var id) || TryParseUserId(lookupInput, out id))
        {
            var cachedUser = Context.Guild.GetUser(id);
            if (cachedUser != null)
            {
                return cachedUser;
            }

            try
            {
                var restUser = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, id);
                if (restUser != null)
                {
                    return restUser;
                }
            }
            catch
            {
                // Ignora erro de busca por REST e segue para fallback global.
            }

            return Context.Client.GetUser(id);
        }

        // Fallback para texto sem mencao: tenta apelido e username.
        var users = Context.Guild.Users;
        var exact = users.FirstOrDefault(u =>
            string.Equals(u.Nickname, lookupInput, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.Username, lookupInput, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        return users.FirstOrDefault(u =>
            (!string.IsNullOrWhiteSpace(u.Nickname) && u.Nickname.Contains(lookupInput, StringComparison.OrdinalIgnoreCase)) ||
            u.Username.Contains(lookupInput, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeLookupInput(string input)
    {
        return input.Trim().TrimStart('@').Trim();
    }

    private static bool TryParseUserId(string input, out ulong userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (MentionUtils.TryParseUser(input, out userId))
        {
            return true;
        }

        return ulong.TryParse(input.Trim(), out userId);
    }
}

using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class UseCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public UseCommand(
        IEconomyService economy,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _economy = economy;
    }

    [Command("use")]
    [Alias("usar")]
    [Summary("Usa um item consumivel.")]
    public async Task UseAsync([Remainder] string? itemName = null)
    {
        await TrackUserAsync();

        if (!string.IsNullOrWhiteSpace(itemName))
        {
            var result = await _economy.UseAsync(Context.User.Id, Context.User.Username, itemName, 1);
            var embed = result.Success
                ? EmbedHelper.CreateSuccess("✨ Item usado", result.Message)
                : EmbedHelper.CreateWarning("✨ Nao foi possivel usar", result.Message);

            await ReplyMajesticAsync(embed);
            return;
        }

        var items = await _economy.GetInventoryAsync(Context.User.Id, Context.User.Username, 0, 25);
        if (items.Items.Count == 0)
        {
            await ReplyAsync("Seu inventario esta vazio.");
            return;
        }

        var embedShop = EmbedHelper.CreateInfo("✨ Usar item", "Selecione um item consumivel.")
            .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId($"use:select:{Context.User.Id}")
            .WithPlaceholder("Escolha um item...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var item in items.Items)
        {
            embedShop.AddField(item.Name, $"Qtd: {item.Quantity} | Venda: {item.SellPrice}\n{item.Description}");
            select.AddOption(item.Name, item.Name, $"{item.Quantity} disponiveis");
        }

        var components = EmbedHelper.BuildCv2Card(embedShop, c =>
        {
            c.WithActionRow(new[] { select });
        });

        await ReplyAsync(components: components);
    }
}

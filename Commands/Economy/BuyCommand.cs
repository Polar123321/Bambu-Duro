using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class BuyCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public BuyCommand(
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

    [Command("buy")]
    [Alias("comprar")]
    [Summary("Compra itens da loja.")]
    public async Task BuyAsync([Remainder] string? itemName = null)
    {
        await TrackUserAsync();

        if (!string.IsNullOrWhiteSpace(itemName))
        {
            var result = await _economy.BuyAsync(Context.User.Id, Context.User.Username, itemName, 1);
            var embed = result.Success
                ? EmbedHelper.CreateSuccess("🛒 Compra realizada", result.Message)
                : EmbedHelper.CreateWarning("🛒 Compra falhou", result.Message);

            await ReplyMajesticAsync(embed);
            return;
        }

        var items = await _economy.GetShopAsync(0, 25);
        if (items.Count == 0)
        {
            await ReplyAsync("Nao ha itens disponiveis na loja.");
            return;
        }

        var embedShop = EmbedHelper.CreateInfo("🛒 Comprar item", "Selecione um item no menu abaixo.")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId("buy:select")
            .WithPlaceholder("Escolha um item...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var item in items)
        {
            embedShop.AddField(item.Name, $"{item.Description}\nCompra: {item.BuyPrice} | Venda: {item.SellPrice}");
            select.AddOption(item.Name, item.Name, $"{item.BuyPrice} moedas");
        }

        var components = EmbedHelper.BuildCv2Card(embedShop, c =>
        {
            c.WithActionRow(new[] { select });
        });

        await ReplyAsync(components: components);
    }
}

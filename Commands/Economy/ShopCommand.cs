using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class ShopCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public ShopCommand(
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

    [Command("shop")]
    [Alias("loja")]
    [Summary("Mostra os itens disponiveis na loja.")]
    public async Task ShopAsync(int page = 1)
    {
        await TrackUserAsync();

        const int pageSize = 5;
        var safePage = Math.Max(1, page);
        var items = await _economy.GetShopAsync((safePage - 1) * pageSize, pageSize);

        if (items.Count == 0)
        {
            await ReplyAsync("Nao ha itens para mostrar.");
            return;
        }

        var embed = EmbedHelper.CreateInfo($"🛒 Loja - Pagina {safePage}",
                "Selecione um item no menu para comprar rapido.")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId($"shop:select:{safePage}")
            .WithPlaceholder("Escolha um item para comprar...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var item in items)
        {
            var line = $"Compra: {item.BuyPrice} | Venda: {item.SellPrice}";
            embed.AddField(item.Name, $"{item.Description}\n{line}");

            select.AddOption(new SelectMenuOptionBuilder()
                .WithLabel(item.Name)
                .WithValue(item.Name)
                .WithDescription($"{item.BuyPrice} moedas"));
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"shop:prev:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"shop:next:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(items.Count < pageSize)
            });
        });

        await ReplyAsync(components: components);
    }
}

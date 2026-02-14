using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Economy;

public sealed class InventoryCommand : CommandBase
{
    private readonly IEconomyService _economy;

    public InventoryCommand(
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

    [Command("inventario")]
    [Alias("inventory", "inv")]
    [Summary("Mostra os itens do seu inventario.")]
    public async Task InventoryAsync(int page = 1)
    {
        await TrackUserAsync();

        const int pageSize = 5;
        var safePage = Math.Max(1, page);
        var result = await _economy.GetInventoryAsync(Context.User.Id, Context.User.Username, (safePage - 1) * pageSize, pageSize);

        if (result.Items.Count == 0)
        {
            await ReplyAsync("Seu inventario esta vazio.");
            return;
        }

        var totalPages = (int)Math.Ceiling(result.TotalItems / (double)pageSize);

        var embed = EmbedHelper.CreateInfo($"🎒 Inventario - Pagina {safePage}", "Selecione um item para usar ou vender.")
            .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId($"inv:select:{safePage}:{Context.User.Id}")
            .WithPlaceholder("Escolha um item...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var item in result.Items)
        {
            embed.AddField(item.Name, $"Qtd: {item.Quantity} | Venda: {item.SellPrice}\n{item.Description}");
            select.AddOption(item.Name, item.Name, $"Qtd: {item.Quantity}");
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"inv:prev:{safePage}:{Context.User.Id}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"inv:next:{safePage}:{Context.User.Id}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= Math.Max(totalPages, 1))
            });
        });

        await ReplyAsync(components: components);
    }
}

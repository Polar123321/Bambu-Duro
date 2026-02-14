using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Economy;

public sealed class ShopInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IEconomyService _economy;
    private readonly EmbedHelper _embeds;

    public ShopInteractions(IEconomyService economy, EmbedHelper embeds)
    {
        _economy = economy;
        _embeds = embeds;
    }

    [ComponentInteraction("shop:select:*")]
    public async Task SelectAsync(string page, string[] selections)
    {
        if (selections.Length == 0)
        {
            await RespondAsync("Selecione um item.", ephemeral: true);
            return;
        }

        var itemName = selections[0];
        var embed = _embeds.CreateInfo("🛒 Comprar item",
            $"Você selecionou **{itemName}**. Escolha a quantidade:");

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Comprar 1")
                    .WithCustomId($"shop:buy:1:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Comprar 5")
                    .WithCustomId($"shop:buy:5:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Quantidade...")
                    .WithCustomId($"shop:qty:{itemName}")
                    .WithStyle(ButtonStyle.Primary),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId("shop:cancel")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await RespondAsync(components: components, ephemeral: true);
    }

    [ComponentInteraction("shop:buy:*:*")]
    public async Task BuyAsync(string quantityRaw, string itemName)
    {
        if (!int.TryParse(quantityRaw, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.BuyAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var title = result.Success ? "✅ Compra realizada" : "⚠️ Compra falhou";
        var embed = result.Success
            ? _embeds.CreateSuccess(title, result.Message)
            : _embeds.CreateWarning(title, result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("shop:qty:*")]
    public async Task QuantityAsync(string itemName)
    {
        await RespondWithModalAsync<QuantityModal>($"shop:qtymodal:{itemName}");
    }

    [ModalInteraction("shop:qtymodal:*")]
    public async Task QuantityModalAsync(string itemName, QuantityModal modal)
    {
        if (!int.TryParse(modal.Quantity, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.BuyAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var title = result.Success ? "✅ Compra realizada" : "⚠️ Compra falhou";
        var embed = result.Success
            ? _embeds.CreateSuccess(title, result.Message)
            : _embeds.CreateWarning(title, result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("shop:cancel")]
    public async Task CancelAsync()
    {
        await RespondAsync("Compra cancelada.", ephemeral: true);
    }

    [ComponentInteraction("shop:prev:*")]
    public async Task PrevPageAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateShopAsync(Math.Max(1, page - 1));
    }

    [ComponentInteraction("shop:next:*")]
    public async Task NextPageAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateShopAsync(page + 1);
    }

    private async Task UpdateShopAsync(int page)
    {
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar a loja.", ephemeral: true);
            return;
        }

        const int pageSize = 5;
        var items = await _economy.GetShopAsync((page - 1) * pageSize, pageSize);
        if (items.Count == 0)
        {
            await RespondAsync("Nao ha itens nessa pagina.", ephemeral: true);
            return;
        }

        var embed = _embeds.CreateInfo($"🛒 Loja - Pagina {page}",
                "Selecione um item no menu para comprar rapido.")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId($"shop:select:{page}")
            .WithPlaceholder("Escolha um item para comprar...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var item in items)
        {
            var line = $"Compra: {item.BuyPrice} | Venda: {item.SellPrice}";
            embed.AddField(item.Name, $"{item.Description}\n{line}");

            select.AddOption(item.Name, item.Name, $"{item.BuyPrice} moedas");
        }

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"shop:prev:{page}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(page <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"shop:next:{page}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(items.Count < pageSize)
            });
        });

        await component.UpdateAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });
    }

    public class QuantityModal : IModal
    {
        public string Title => "Quantidade";

        [InputLabel("Quantidade")]
        [ModalTextInput("quantity", placeholder: "Ex: 3", maxLength: 3)]
        public string Quantity { get; set; } = "1";
    }
}

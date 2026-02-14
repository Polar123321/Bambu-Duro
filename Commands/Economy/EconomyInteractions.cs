using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Economy;

public sealed class EconomyInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IEconomyService _economy;
    private readonly EmbedHelper _embeds;

    public EconomyInteractions(IEconomyService economy, EmbedHelper embeds)
    {
        _economy = economy;
        _embeds = embeds;
    }

    [ComponentInteraction("buy:select")]
    public async Task BuySelectAsync(string[] selections)
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
                    .WithCustomId($"buy:do:1:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Comprar 5")
                    .WithCustomId($"buy:do:5:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Quantidade...")
                    .WithCustomId($"buy:qty:{itemName}")
                    .WithStyle(ButtonStyle.Primary),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId("buy:cancel")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await RespondAsync(components: components, ephemeral: true);
    }

    [ComponentInteraction("buy:do:*:*")]
    public async Task BuyDoAsync(string quantityRaw, string itemName)
    {
        if (!int.TryParse(quantityRaw, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.BuyAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var embed = result.Success
            ? _embeds.CreateSuccess("✅ Compra realizada", result.Message)
            : _embeds.CreateWarning("⚠️ Compra falhou", result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("buy:qty:*")]
    public async Task BuyQtyAsync(string itemName)
    {
        await RespondWithModalAsync<QuantityModal>($"buy:qtymodal:{itemName}");
    }

    [ModalInteraction("buy:qtymodal:*")]
    public async Task BuyQtyModalAsync(string itemName, QuantityModal modal)
    {
        if (!int.TryParse(modal.Quantity, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.BuyAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var embed = result.Success
            ? _embeds.CreateSuccess("✅ Compra realizada", result.Message)
            : _embeds.CreateWarning("⚠️ Compra falhou", result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("buy:cancel")]
    public async Task BuyCancelAsync()
    {
        await RespondAsync("Compra cancelada.", ephemeral: true);
    }

    [ComponentInteraction("sell:select:*")]
    public async Task SellSelectAsync(string userIdRaw, string[] selections)
    {
        if (!ulong.TryParse(userIdRaw, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("Esse inventario nao e seu.", ephemeral: true);
            return;
        }

        if (selections.Length == 0)
        {
            await RespondAsync("Selecione um item.", ephemeral: true);
            return;
        }

        var itemName = selections[0];
        var embed = _embeds.CreateInfo("💸 Vender item",
            $"Você selecionou **{itemName}**. Escolha a quantidade:");

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Vender 1")
                    .WithCustomId($"sell:do:1:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Vender 5")
                    .WithCustomId($"sell:do:5:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Quantidade...")
                    .WithCustomId($"sell:qty:{itemName}")
                    .WithStyle(ButtonStyle.Primary),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId("sell:cancel")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await RespondAsync(components: components, ephemeral: true);
    }

    [ComponentInteraction("sell:do:*:*")]
    public async Task SellDoAsync(string quantityRaw, string itemName)
    {
        if (!int.TryParse(quantityRaw, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.SellAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var embed = result.Success
            ? _embeds.CreateSuccess("✅ Venda realizada", result.Message)
            : _embeds.CreateWarning("⚠️ Venda falhou", result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("sell:qty:*")]
    public async Task SellQtyAsync(string itemName)
    {
        await RespondWithModalAsync<QuantityModal>($"sell:qtymodal:{itemName}");
    }

    [ModalInteraction("sell:qtymodal:*")]
    public async Task SellQtyModalAsync(string itemName, QuantityModal modal)
    {
        if (!int.TryParse(modal.Quantity, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.SellAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var embed = result.Success
            ? _embeds.CreateSuccess("✅ Venda realizada", result.Message)
            : _embeds.CreateWarning("⚠️ Venda falhou", result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("sell:cancel")]
    public async Task SellCancelAsync()
    {
        await RespondAsync("Venda cancelada.", ephemeral: true);
    }

    [ComponentInteraction("use:select:*")]
    public async Task UseSelectAsync(string userIdRaw, string[] selections)
    {
        if (!ulong.TryParse(userIdRaw, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("Esse inventario nao e seu.", ephemeral: true);
            return;
        }

        if (selections.Length == 0)
        {
            await RespondAsync("Selecione um item.", ephemeral: true);
            return;
        }

        var itemName = selections[0];
        var embed = _embeds.CreateInfo("✨ Usar item",
            $"Você selecionou **{itemName}**. Escolha a quantidade:");

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Usar 1")
                    .WithCustomId($"use:do:1:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Usar 3")
                    .WithCustomId($"use:do:3:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Quantidade...")
                    .WithCustomId($"use:qty:{itemName}")
                    .WithStyle(ButtonStyle.Primary),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId("use:cancel")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await RespondAsync(components: components, ephemeral: true);
    }

    [ComponentInteraction("use:do:*:*")]
    public async Task UseDoAsync(string quantityRaw, string itemName)
    {
        if (!int.TryParse(quantityRaw, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.UseAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var embed = result.Success
            ? _embeds.CreateSuccess("✅ Item usado", result.Message)
            : _embeds.CreateWarning("⚠️ Falha ao usar", result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("use:qty:*")]
    public async Task UseQtyAsync(string itemName)
    {
        await RespondWithModalAsync<QuantityModal>($"use:qtymodal:{itemName}");
    }

    [ModalInteraction("use:qtymodal:*")]
    public async Task UseQtyModalAsync(string itemName, QuantityModal modal)
    {
        if (!int.TryParse(modal.Quantity, out var quantity))
        {
            await RespondAsync("Quantidade inválida.", ephemeral: true);
            return;
        }

        var result = await _economy.UseAsync(Context.User.Id, Context.User.Username, itemName, quantity);
        var embed = result.Success
            ? _embeds.CreateSuccess("✅ Item usado", result.Message)
            : _embeds.CreateWarning("⚠️ Falha ao usar", result.Message);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("use:cancel")]
    public async Task UseCancelAsync()
    {
        await RespondAsync("Uso cancelado.", ephemeral: true);
    }

    [ComponentInteraction("inv:select:*:*")]
    public async Task InventorySelectAsync(string pageRaw, string userIdRaw, string[] selections)
    {
        if (!ulong.TryParse(userIdRaw, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("Esse inventario nao e seu.", ephemeral: true);
            return;
        }

        if (selections.Length == 0)
        {
            await RespondAsync("Selecione um item.", ephemeral: true);
            return;
        }

        var itemName = selections[0];
        var embed = _embeds.CreateInfo("🎒 Item selecionado",
            $"Você selecionou **{itemName}**. O que deseja fazer?");

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Usar")
                    .WithCustomId($"use:qty:{itemName}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Vender")
                    .WithCustomId($"sell:qty:{itemName}")
                    .WithStyle(ButtonStyle.Primary),
                new ButtonBuilder()
                    .WithLabel("Cancelar")
                    .WithCustomId("inv:cancel")
                    .WithStyle(ButtonStyle.Secondary)
            });
        });

        await RespondAsync(components: components, ephemeral: true);
    }

    [ComponentInteraction("inv:prev:*:*")]
    public async Task InventoryPrevAsync(string pageRaw, string userIdRaw)
    {
        if (!ulong.TryParse(userIdRaw, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("Esse inventario nao e seu.", ephemeral: true);
            return;
        }

        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateInventoryAsync(Math.Max(1, page - 1), userId);
    }

    [ComponentInteraction("inv:next:*:*")]
    public async Task InventoryNextAsync(string pageRaw, string userIdRaw)
    {
        if (!ulong.TryParse(userIdRaw, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("Esse inventario nao e seu.", ephemeral: true);
            return;
        }

        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateInventoryAsync(page + 1, userId);
    }

    [ComponentInteraction("inv:cancel")]
    public async Task InventoryCancelAsync()
    {
        await RespondAsync("Operacao cancelada.", ephemeral: true);
    }

    private async Task UpdateInventoryAsync(int page, ulong userId)
    {
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar o inventario.", ephemeral: true);
            return;
        }

        const int pageSize = 5;
        var result = await _economy.GetInventoryAsync(userId, Context.User.Username, (page - 1) * pageSize, pageSize);
        if (result.Items.Count == 0)
        {
            await RespondAsync("Nao ha itens nessa pagina.", ephemeral: true);
            return;
        }

        var totalPages = (int)Math.Ceiling(result.TotalItems / (double)pageSize);
        var safePage = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        var embed = _embeds.CreateInfo($"🎒 Inventario - Pagina {safePage}", "Selecione um item para usar ou vender.")
            .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId($"inv:select:{safePage}:{userId}")
            .WithPlaceholder("Escolha um item...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var item in result.Items)
        {
            embed.AddField(item.Name, $"Qtd: {item.Quantity} | Venda: {item.SellPrice}\n{item.Description}");
            select.AddOption(item.Name, item.Name, $"Qtd: {item.Quantity}");
        }

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"inv:prev:{safePage}:{userId}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"inv:next:{safePage}:{userId}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= totalPages)
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

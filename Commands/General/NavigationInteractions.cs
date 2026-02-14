using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.General;

public sealed class NavigationInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EmbedHelper _embeds;
    private readonly IOptions<BotConfiguration> _config;

    public NavigationInteractions(EmbedHelper embeds, IOptions<BotConfiguration> config)
    {
        _embeds = embeds;
        _config = config;
    }

    [ComponentInteraction("nav:help")]
    public Task HelpAsync() => RespondShortcutAsync("Ajuda", $"Use `{_config.Value.Prefix}help` para ver comandos.");

    [ComponentInteraction("nav:config")]
    public Task ConfigAsync() => RespondShortcutAsync("Config", $"Use `{_config.Value.Prefix}config` para abrir o painel.");

    [ComponentInteraction("nav:shop")]
    public Task ShopAsync() => RespondShortcutAsync("Loja", $"Use `{_config.Value.Prefix}shop` para abrir a loja.");

    [ComponentInteraction("nav:inv")]
    public Task InventoryAsync() => RespondShortcutAsync("Inventario", $"Use `{_config.Value.Prefix}inv` para ver seu inventario.");

    [ComponentInteraction("nav:menu")]
    public async Task MenuAsync()
    {
        var embed = _embeds.CreateMajestic("Atalhos", "Escolha uma opcao abaixo.");

        var select = new SelectMenuBuilder()
            .WithCustomId("nav:select")
            .WithPlaceholder("Acoes rapidas...")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("Ajuda", "help", "Ver comandos")
            .AddOption("Config", "config", "Configurar servidor")
            .AddOption("Loja", "shop", "Abrir loja")
            .AddOption("Inventario", "inv", "Ver inventario")
            .AddOption("Staff", "staff", "Abrir formulario")
            .AddOption("Perfil", "perfil", "Seu perfil");

        await RespondAsync(components: _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
        }), ephemeral: true);
    }

    [ComponentInteraction("nav:select")]
    public async Task SelectAsync(string[] selections)
    {
        if (selections.Length == 0)
        {
            await RespondAsync("Selecione uma opcao.", ephemeral: true);
            return;
        }

        var key = selections[0];
        var response = key switch
        {
            "help" => $"Use `{_config.Value.Prefix}help` para ver comandos.",
            "config" => $"Use `{_config.Value.Prefix}config` para abrir o painel.",
            "shop" => $"Use `{_config.Value.Prefix}shop` para abrir a loja.",
            "inv" => $"Use `{_config.Value.Prefix}inv` para ver seu inventario.",
            "staff" => $"Use `{_config.Value.Prefix}staff` para o formulario de staff.",
            "perfil" => $"Use `{_config.Value.Prefix}perfil` para seu perfil.",
            _ => "Comando indisponivel."
        };

        await RespondAsync(components: _embeds.BuildCv2(_embeds.CreateMajestic("Atalho", response)), ephemeral: true);
    }

    private Task RespondShortcutAsync(string title, string message)
    {
        return RespondAsync(components: _embeds.BuildCv2(_embeds.CreateMajestic(title, message)), ephemeral: true);
    }
}

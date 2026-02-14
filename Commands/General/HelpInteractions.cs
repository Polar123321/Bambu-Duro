using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Helpers;

namespace ConsoleApp4.Commands.General;

public sealed class HelpInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EmbedHelper _embeds;
    private readonly IOptions<BotConfiguration> _config;
    private readonly CommandService _commands;

    public HelpInteractions(EmbedHelper embeds, IOptions<BotConfiguration> config, CommandService commands)
    {
        _embeds = embeds;
        _config = config;
        _commands = commands;
    }

    [ComponentInteraction("help:prev:*")]
    public async Task PrevAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        var next = Math.Max(1, page - 1);
        await UpdateHelpAsync(next);
    }

    [ComponentInteraction("help:next:*")]
    public async Task NextAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        var next = page + 1;
        await UpdateHelpAsync(next);
    }

    [ComponentInteraction("help:how")]
    public async Task HowAsync()
    {
        var embed = _embeds.CreateInfo("Como usar o help",
                $"- `{_config.Value.Prefix}help` abre a lista.\n" +
                $"- `{_config.Value.Prefix}help 2` muda de pagina.\n" +
                $"- `{_config.Value.Prefix}help <comando>` mostra detalhes.")
            .WithCurrentTimestamp();

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    private async Task UpdateHelpAsync(int page)
    {
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar a pagina.", ephemeral: true);
            return;
        }

        const int pageSize = 4;
        var modules = _commands.Modules
            .Where(m => m.Commands.Count > 0)
            .OrderBy(m => m.Name)
            .ToList();

        var totalPages = (int)Math.Ceiling(modules.Count / (double)pageSize);
        var safePage = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        var embed = _embeds.CreateInfo("Central de Ajuda",
                $"Use `{_config.Value.Prefix}help <comando>` para detalhes.\n" +
                $"Pagina {safePage}/{Math.Max(totalPages, 1)}")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        foreach (var module in modules.Skip((safePage - 1) * pageSize).Take(pageSize))
        {
            var icon = module.Name switch
            {
                "General" => "General",
                "Fun" => "Fun",
                "Economy" => "Economy",
                "Rpg" => "Rpg",
                "User" => "User",
                "Moderation" => "Moderation",
                _ => "Other"
            };

            var lines = module.Commands
                .OrderBy(c => c.Name)
                .Select(c => $"`{_config.Value.Prefix}{c.Aliases.First()}` - {c.Summary ?? "Sem descricao disponivel"}")
                .ToArray();

            embed.AddField($"{icon} {module.Name}", string.Join("\n", lines));
        }

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"help:prev:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"help:next:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= totalPages),
                new ButtonBuilder()
                    .WithLabel("🔎")
                    .WithCustomId("help:how")
                    .WithStyle(ButtonStyle.Primary)
            });
        });

        await component.UpdateAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });
    }
}

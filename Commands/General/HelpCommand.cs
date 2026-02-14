using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.General;

public sealed class HelpCommand : CommandBase
{
    private readonly CommandService _commands;

    private static readonly Dictionary<string, string> ModuleIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        { "General", "General" },
        { "Fun", "Fun" },
        { "Economy", "Economy" },
        { "Rpg", "Rpg" },
        { "User", "User" },
        { "Moderation", "Moderation" }
    };

    public HelpCommand(
        CommandService commands,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _commands = commands;
    }

    [Command("ajuda")]
    [Alias("help", "commands")]
    [Summary("Mostra ajuda e lista de comandos.")]
    public async Task HelpAsync([Remainder] string? input = null)
    {
        await TrackUserAsync();

        if (TryResolvePage(input, out var page))
        {
            await SendPagedHelpAsync(page);
            return;
        }

        if (!string.IsNullOrWhiteSpace(input))
        {
            await SendCommandHelpAsync(input.Trim());
            return;
        }

        await SendPagedHelpAsync(1);
    }

    private async Task SendCommandHelpAsync(string commandName)
    {
        var command = _commands.Commands.FirstOrDefault(c =>
            c.Aliases.Any(a => string.Equals(a, commandName, StringComparison.OrdinalIgnoreCase)));

        if (command == null)
        {
            await ReplyAsync($"Comando '{commandName}' nao encontrado. Use {Config.Value.Prefix}help.");
            return;
        }

        var aliases = string.Join(", ", command.Aliases.Select(a => $"`{Config.Value.Prefix}{a}`"));
        var usage = BuildUsage(command);
        var parameters = command.Parameters.Count == 0
            ? "Nenhum"
            : string.Join("\n", command.Parameters.Select(p => $"`{p.Name}` ({p.Type.Name})"));

        var embed = EmbedHelper.CreateInfo($"Ajuda: {command.Name}", command.Summary ?? "Sem descricao disponivel.")
            .AddField("Uso", usage, false)
            .AddField("Aliases", aliases, false)
            .AddField("Parametros", parameters, false)
            .WithFooter("Dica: use !help <pagina> para ver categorias")
            .WithCurrentTimestamp();

        await ReplyMajesticAsync(embed);
    }

    private async Task SendPagedHelpAsync(int page)
    {
        const int pageSize = 4;
        var modules = _commands.Modules
            .Where(m => m.Commands.Count > 0)
            .OrderBy(m => m.Name)
            .ToList();

        var totalPages = (int)Math.Ceiling(modules.Count / (double)pageSize);
        var safePage = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        var embed = EmbedHelper.CreateInfo("Central de Ajuda",
                $"Use `{Config.Value.Prefix}help <comando>` para detalhes. Ex: `{Config.Value.Prefix}help work`.\n" +
                $"Pagina {safePage}/{Math.Max(totalPages, 1)}")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        foreach (var module in modules.Skip((safePage - 1) * pageSize).Take(pageSize))
        {
            var icon = ModuleIcons.TryGetValue(module.Name, out var value) ? value : "Caixa";
            var lines = module.Commands
                .OrderBy(c => c.Name)
                .Select(c => $"`{Config.Value.Prefix}{c.Aliases.First()}` - {c.Summary ?? "Sem descricao disponivel"}")
                .ToArray();

            embed.AddField($"{icon} {module.Name}", string.Join("\n", lines));
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
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
                    .WithDisabled(safePage >= Math.Max(totalPages, 1)),
                new ButtonBuilder()
                    .WithLabel("🔎")
                    .WithCustomId("help:how")
                    .WithStyle(ButtonStyle.Primary)
            });
        });

        await ReplyAsync(components: components);
    }

    private static bool TryResolvePage(string? input, out int page)
    {
        page = 1;
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        return int.TryParse(input.Trim(), out page) && page > 0;
    }

    private string BuildUsage(CommandInfo command)
    {
        var firstAlias = command.Aliases.First();
        if (command.Parameters.Count == 0)
        {
            return $"`{Config.Value.Prefix}{firstAlias}`";
        }

        var parts = command.Parameters.Select(p =>
        {
            var name = p.Name ?? "param";
            return p.IsOptional ? $"[{name}]" : $"<{name}>";
        });

        return $"`{Config.Value.Prefix}{firstAlias} {string.Join(" ", parts)}`";
    }
}

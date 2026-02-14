using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Fun;

public sealed class ShipSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IShipCompatibilityService _ship;
    private readonly EmbedHelper _embeds;
    private readonly IShipStore _store;

    public ShipSlashCommand(IShipCompatibilityService ship, EmbedHelper embeds, IShipStore store)
    {
        _ship = ship;
        _embeds = embeds;
        _store = store;
    }

    [SlashCommand("ship", "Calcula compatibilidade entre dois membros com base em interacoes e horarios ativos.")]
    public async Task ShipAsync(
        [Summary("user1", "Primeiro membro")] SocketGuildUser user1,
        [Summary("user2", "Segundo membro (opcional)")] SocketGuildUser? user2 = null)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Esse comando so funciona em servidores.", ephemeral: true);
            return;
        }

        var a = user2 == null ? (SocketGuildUser)Context.User : user1;
        var b = user2 == null ? user1 : user2;

        if (a.Id == b.Id)
        {
            await RespondAsync("Voce nao pode shipar a mesma pessoa.", ephemeral: true);
            return;
        }

        var id1 = Math.Min(a.Id, b.Id);
        var id2 = Math.Max(a.Id, b.Id);

        var stored = await _store.GetAsync(id1, id2);
        var isManual = stored?.IsManual == true;

        var result = await _ship.CalculateAsync((SocketGuild)Context.Guild, id1, id2);
        var percent = isManual ? stored!.Compatibility : result.Percent;

        var coupleName = BuildCoupleName(a.Username, b.Username);
        var bar = BuildBar(percent);

        var embed = _embeds.CreateInfo("💘 /ship", isManual ? "Compatibilidade definida manualmente" : result.Summary)
            .AddField("Casal", $"{a.Mention} + {b.Mention}", false)
            .AddField("Ship", coupleName, true)
            .AddField("Compatibilidade", $"{percent}% {bar}", false);

        if (!isManual)
        {
            embed.AddField("Canais em comum", result.SharedChannelsText, false)
                .AddField("Horarios ativos", result.ActiveHoursText, false)
                .AddField("Confianca", result.ConfidenceText, true)
                .AddField("Clima", result.Title, true);
        }

        await RespondAsync(components: _embeds.BuildCv2(embed));
    }

    private static string BuildCoupleName(string name1, string name2)
    {
        var left = name1.Length <= 3 ? name1 : name1[..(name1.Length / 2)];
        var right = name2.Length <= 3 ? name2 : name2[(name2.Length / 2)..];
        return left + right;
    }

    private static string BuildBar(int percent)
    {
        var total = 10;
        var filled = (int)Math.Round(percent / 10.0);
        return "[" + new string('█', filled) + new string('░', total - filled) + "]";
    }
}

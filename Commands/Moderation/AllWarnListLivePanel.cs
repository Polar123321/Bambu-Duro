using System.Collections.Concurrent;
using System.Text;
using Discord;
using ConsoleApp4.Helpers;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Moderation;

internal static class AllWarnListLivePanel
{
    private const int PageSize = 10;
    private const int MaxReasonLength = 120;
    private static readonly ConcurrentDictionary<ulong, (ulong ChannelId, ulong MessageId)> Panels = new();

    public static void Register(ulong guildId, ulong channelId, ulong messageId)
    {
        Panels[guildId] = (channelId, messageId);
    }

    public static async Task TryRefreshAsync(IDiscordClient client, EmbedHelper embeds, IWarnService warns, ulong guildId)
    {
        if (!Panels.TryGetValue(guildId, out var panel))
        {
            return;
        }

        try
        {
            var channel = await client.GetChannelAsync(panel.ChannelId).ConfigureAwait(false) as IMessageChannel;
            if (channel == null)
            {
                Panels.TryRemove(guildId, out _);
                return;
            }

            var message = await channel.GetMessageAsync(panel.MessageId).ConfigureAwait(false) as IUserMessage;
            if (message == null)
            {
                Panels.TryRemove(guildId, out _);
                return;
            }

            var allWarns = await warns.GetAllActiveWarnsAsync(guildId).ConfigureAwait(false);
            var components = BuildPublicComponents(embeds, allWarns, page: 1);
            await message.ModifyAsync(props =>
            {
                props.Components = components;
                props.Embeds = Array.Empty<Embed>();
            }).ConfigureAwait(false);
        }
        catch
        {
            
            Panels.TryRemove(guildId, out _);
        }
    }

    public static MessageComponent BuildPublicComponents(EmbedHelper embeds, IReadOnlyList<WarnEntry> warns, int page)
    {
        var totalPages = (int)Math.Ceiling(warns.Count / (double)PageSize);
        var safePage = warns.Count == 0
            ? 1
            : Math.Clamp(page, 1, Math.Max(totalPages, 1));

        var startIndex = (safePage - 1) * PageSize;
        var pageWarns = warns.Skip(startIndex).Take(PageSize).ToList();
        var body = warns.Count == 0 ? "Nenhum warn ativo neste servidor." : BuildDescription(pageWarns, startIndex);

        var embed = embeds.CreateWarning("Warns ativos do servidor",
                $"Total de warns ativos: **{warns.Count}**\nPagina {safePage}/{Math.Max(totalPages, 1)}\n\n{body}\n\nClique em **Navegar (privado)** para paginar so para voce.")
            .WithCurrentTimestamp();

        return embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Navegar (privado)")
                    .WithCustomId($"allwarn:open:{safePage}")
                    .WithStyle(ButtonStyle.Primary)
            });
        });
    }

    private static string BuildDescription(IReadOnlyList<WarnEntry> warns, int startIndex)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < warns.Count; i++)
        {
            var warn = warns[i];
            var reason = NormalizeReason(warn.Reason);

            sb.AppendLine($"`#{startIndex + i + 1}` <@{warn.DiscordUserId}>");
            sb.AppendLine($"Motivo: {reason}");
            sb.AppendLine($"Moderador: <@{warn.DiscordModeratorId}> | Em: {warn.CreatedAtUtc:dd/MM/yyyy HH:mm} UTC");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string NormalizeReason(string reason)
    {
        var safe = string.IsNullOrWhiteSpace(reason) ? "(sem motivo)" : reason.Trim();
        if (safe.Length <= MaxReasonLength)
        {
            return safe;
        }

        return $"{safe[..MaxReasonLength]}...";
    }
}

using System.Diagnostics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class RecountUserMessagesCommand : CommandBase
{
    private readonly IUserGuildStatsService _stats;

    public RecountUserMessagesCommand(
        IUserGuildStatsService stats,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _stats = stats;
    }

    [Command("recontarmensagensusuario")]
    [Alias("recontarmensagensuser", "recountusermessages", "recountuser")]
    [Summary("Reconta todas as mensagens de um usuario (historico completo).")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RecountUserAsync(string target, ITextChannel? channel = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var guild = Context.Guild;
        var botUser = guild.CurrentUser;
        if (botUser == null)
        {
            await ReplyAsync("Nao consegui identificar as permissoes do bot.");
            return;
        }

        var resolvedUser = await ResolveTargetAsync(guild, target);
        if (resolvedUser == null)
        {
            await ReplyAsync("Nao encontrei o usuario informado neste servidor.");
            return;
        }

        var status = await ReplyAsync(channel == null
            ? $"Iniciando recontagem de mensagens de {resolvedUser.Mention}. Isso pode demorar..."
            : $"Iniciando recontagem de mensagens de {resolvedUser.Mention} em {channel.Mention}. Isso pode demorar...");

        var totalMessages = 0;
        var channelsScanned = 0;
        var channelsSkipped = 0;
        var stopwatch = Stopwatch.StartNew();
        var delayState = new DelayState(250);
        var lastStatusUpdate = new LastUpdateState(DateTime.UtcNow);

        var channels = channel == null
            ? guild.TextChannels.Cast<ITextChannel>()
            : new[] { channel };

        foreach (var textChannel in channels)
        {
            var socketChannel = textChannel as SocketTextChannel;
            if (socketChannel == null)
            {
                channelsSkipped++;
                continue;
            }

            var perms = botUser.GetPermissions(socketChannel);
            if (!perms.ViewChannel || !perms.ReadMessageHistory)
            {
                channelsSkipped++;
                continue;
            }

            var channelCount = await CountChannelMessagesAsync(
                socketChannel,
                resolvedUser.Id,
                delayState,
                progress => UpdateStatusAsync(status, progress, channelsScanned + 1, channelsSkipped, totalMessages, stopwatch, lastStatusUpdate));
            totalMessages += channelCount;
            channelsScanned++;

            await _stats.ReplaceUserChannelMessageCountAsync(guild.Id, socketChannel.Id, resolvedUser.Id, channelCount, adjustGuildTotals: true);
        }

        stopwatch.Stop();

        var embed = EmbedHelper.CreateInfo("Recontagem concluida",
                $"Servidor: **{guild.Name}**\n" +
                $"Usuario: **{resolvedUser.Username}**\n" +
                $"Canais lidos: **{channelsScanned}** | Pulados: **{channelsSkipped}**\n" +
                $"Mensagens processadas: **{totalMessages:N0}**")
            .WithCurrentTimestamp();

        await ReplyMajesticAsync(embed);
    }

    private static async Task<int> CountChannelMessagesAsync(
        SocketTextChannel channel,
        ulong userId,
        DelayState delayState,
        Func<ProgressSnapshot, Task> onProgress)
    {
        var total = 0;
        ulong? beforeId = null;
        var batches = 0;

        while (true)
        {
            List<IMessage> batch;
            var fetchSw = Stopwatch.StartNew();
            try
            {
                batch = beforeId == null
                    ? (await channel.GetMessagesAsync(100).FlattenAsync()).ToList()
                    : (await channel.GetMessagesAsync(beforeId.Value, Direction.Before, 100).FlattenAsync()).ToList();
            }
            catch (Exception)
            {
                delayState.Value = Math.Min(2500, delayState.Value + 500);
                await Task.Delay(delayState.Value);
                continue;
            }
            fetchSw.Stop();
            batches++;

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var message in batch)
            {
                if (message.Author.Id != userId)
                {
                    continue;
                }

                if (message.Author.IsBot)
                {
                    continue;
                }

                total++;
            }

            beforeId = batch.Last().Id;
            delayState.Value = AdjustDelay(delayState.Value, fetchSw.ElapsedMilliseconds);
            await SafeProgressAsync(onProgress, new ProgressSnapshot(channel.Id, channel.Name, total, batch.Count, batches, delayState.Value, (int)fetchSw.ElapsedMilliseconds));
            await Task.Delay(delayState.Value);
        }

        return total;
    }

    private static int AdjustDelay(int currentDelayMs, long fetchMs)
    {
        const int minDelay = 100;
        const int maxDelay = 2000;

        var newDelay = currentDelayMs;

        if (fetchMs >= 1200)
        {
            newDelay = Math.Min(maxDelay, (int)(currentDelayMs * 1.5) + 200);
        }
        else if (fetchMs <= 400)
        {
            newDelay = Math.Max(minDelay, currentDelayMs - 150);
        }

        return newDelay;
    }

    private static async Task UpdateStatusAsync(
        IUserMessage status,
        ProgressSnapshot progress,
        int channelsScanned,
        int channelsSkipped,
        int totalMessages,
        Stopwatch stopwatch,
        LastUpdateState lastUpdateUtc)
    {
        var now = DateTime.UtcNow;
        if ((now - lastUpdateUtc.Value).TotalMilliseconds < 700)
        {
            return;
        }

        lastUpdateUtc.Value = now;
        var rateHint = progress.FetchMs >= 1500 ? " (possivel rate limit)" : string.Empty;
        var text =
            $"Recontagem em andamento...\n" +
            $"Canal: **#{progress.ChannelName}**\n" +
            $"Mensagens no canal: **{progress.ChannelTotal:N0}**\n" +
            $"Batch: **{progress.Batches}** | Ultimo lote: **{progress.BatchSize}**\n" +
            $"Delay atual: **{progress.DelayMs}ms** | Fetch: **{progress.FetchMs}ms**{rateHint}\n" +
            $"Canais lidos: **{channelsScanned}** | Pulados: **{channelsSkipped}**\n" +
            $"Total parcial: **{totalMessages:N0}**";

        try
        {
            await status.ModifyAsync(m => m.Content = text);
        }
        catch
        {
            // ignore edit failures (rate limit or missing perms)
        }
    }

    private static async Task SafeProgressAsync(Func<ProgressSnapshot, Task> onProgress, ProgressSnapshot snapshot)
    {
        try
        {
            await onProgress(snapshot);
        }
        catch
        {
            // ignore progress errors
        }
    }

    private sealed class DelayState
    {
        public DelayState(int value)
        {
            Value = value;
        }

        public int Value { get; set; }
    }

    private sealed class LastUpdateState
    {
        public LastUpdateState(DateTime value)
        {
            Value = value;
        }

        public DateTime Value { get; set; }
    }

    private readonly record struct ProgressSnapshot(
        ulong ChannelId,
        string ChannelName,
        int ChannelTotal,
        int BatchSize,
        int Batches,
        int DelayMs,
        int FetchMs);

    private async Task<IUser?> ResolveTargetAsync(SocketGuild guild, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (TryParseUserId(input, out var userId))
        {
            var cached = guild.GetUser(userId);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                var rest = await Context.Client.Rest.GetGuildUserAsync(guild.Id, userId);
                return rest;
            }
            catch
            {
                return null;
            }
        }

        var normalized = input.Trim();
        var byNick = guild.Users.FirstOrDefault(u =>
            string.Equals(u.Nickname, normalized, StringComparison.OrdinalIgnoreCase));
        if (byNick != null)
        {
            return byNick;
        }

        var byName = guild.Users.FirstOrDefault(u =>
            string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase));
        return byName;
    }

    private static bool TryParseUserId(string input, out ulong userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (MentionUtils.TryParseUser(input, out userId))
        {
            return true;
        }

        var trimmed = input.Trim();
        return ulong.TryParse(trimmed, out userId);
    }
}

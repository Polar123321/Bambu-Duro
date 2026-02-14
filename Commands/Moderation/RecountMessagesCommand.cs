using System.Diagnostics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class RecountMessagesCommand : CommandBase
{
    private readonly IUserGuildStatsService _stats;

    public RecountMessagesCommand(
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

    [Command("recontarmensagens")]
    [Alias("recontarmsgs", "recountmessages", "recountmsgs")]
    [Summary("Reconta todas as mensagens do servidor (historico completo).")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Priority(2)]
    public async Task RecountAsync(ITextChannel? channel = null, int? dias = null)
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

        var status = await ReplyAsync(channel == null
            ? "Iniciando recontagem de mensagens. Isso pode demorar..."
            : $"Iniciando recontagem de mensagens em {channel.Mention}. Isso pode demorar...");

        var totalCounts = new Dictionary<ulong, int>();
        var channelCounts = new Dictionary<ulong, Dictionary<ulong, int>>();
        var totalMessages = 0;
        var channelsScanned = 0;
        var channelsSkipped = 0;
        var delayState = new DelayState(250);
        var lastStatusUpdate = new LastUpdateState(DateTime.UtcNow);
        var cutoffUtc = dias.HasValue && dias.Value > 0
            ? DateTime.UtcNow.AddDays(-dias.Value)
            : (DateTime?)null;

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

            var (channelCount, userCounts) = await CountChannelMessagesAsync(
                socketChannel,
                delayState,
                cutoffUtc,
                progress => UpdateStatusAsync(status, progress, channelsScanned + 1, channelsSkipped, totalMessages, lastStatusUpdate, cutoffUtc));
            totalMessages += channelCount;
            channelsScanned++;
            channelCounts[socketChannel.Id] = userCounts;

            foreach (var entry in userCounts)
            {
                totalCounts.TryGetValue(entry.Key, out var current);
                totalCounts[entry.Key] = current + entry.Value;
            }
        }

        if (channel == null)
        {
            await _stats.ReplaceMessageCountsAsync(guild.Id, totalCounts);
            foreach (var textChannel in guild.TextChannels)
            {
                if (channelCounts.TryGetValue(textChannel.Id, out var perUser))
                {
                    await _stats.ReplaceChannelMessageCountsAsync(guild.Id, textChannel.Id, perUser, adjustGuildTotals: false);
                }
                else
                {
                    await _stats.ReplaceChannelMessageCountsAsync(guild.Id, textChannel.Id, new Dictionary<ulong, int>(), adjustGuildTotals: false);
                }
            }
        }
        else
        {
            var channelId = channel.Id;
            if (channelCounts.TryGetValue(channelId, out var perUser))
            {
                await _stats.ReplaceChannelMessageCountsAsync(guild.Id, channelId, perUser, adjustGuildTotals: true);
            }
            else
            {
                await _stats.ReplaceChannelMessageCountsAsync(guild.Id, channelId, new Dictionary<ulong, int>(), adjustGuildTotals: true);
            }
        }

        var embed = EmbedHelper.CreateInfo("Recontagem concluida",
                $"Servidor: **{guild.Name}**\n" +
                $"Canais lidos: **{channelsScanned}** | Pulados: **{channelsSkipped}**\n" +
                $"Mensagens processadas: **{totalMessages:N0}**")
            .WithCurrentTimestamp();

        await ReplyMajesticAsync(embed);
    }

    [Command("recontarmensagens")]
    [Alias("recontarmsgs", "recountmessages", "recountmsgs")]
    [Summary("Reconta mensagens de um usuario (historico completo).")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Priority(1)]
    public async Task RecountUserAsync(string target, ITextChannel? channel = null, int? dias = null)
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
        var delayState = new DelayState(250);
        var lastStatusUpdate = new LastUpdateState(DateTime.UtcNow);
        var cutoffUtc = dias.HasValue && dias.Value > 0
            ? DateTime.UtcNow.AddDays(-dias.Value)
            : (DateTime?)null;

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

            var channelCount = await CountUserMessagesAsync(
                socketChannel,
                resolvedUser.Id,
                delayState,
                cutoffUtc,
                progress => UpdateStatusAsync(status, progress, channelsScanned + 1, channelsSkipped, totalMessages, lastStatusUpdate, cutoffUtc));
            totalMessages += channelCount;
            channelsScanned++;

            await _stats.ReplaceUserChannelMessageCountAsync(guild.Id, socketChannel.Id, resolvedUser.Id, channelCount, adjustGuildTotals: true);
        }

        var embed = EmbedHelper.CreateInfo("Recontagem concluida",
                $"Servidor: **{guild.Name}**\n" +
                $"Usuario: **{resolvedUser.Username}**\n" +
                $"Canais lidos: **{channelsScanned}** | Pulados: **{channelsSkipped}**\n" +
                $"Mensagens processadas: **{totalMessages:N0}**")
            .WithCurrentTimestamp();

        await ReplyMajesticAsync(embed);
    }

    private static async Task<(int Total, Dictionary<ulong, int> Counts)> CountChannelMessagesAsync(
        SocketTextChannel channel,
        DelayState delayState,
        DateTime? cutoffUtc,
        Func<ProgressSnapshot, Task> onProgress)
    {
        var total = 0;
        var counts = new Dictionary<ulong, int>();
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

            var reachedCutoff = false;
            foreach (var message in batch)
            {
                if (cutoffUtc.HasValue && message.Timestamp.UtcDateTime < cutoffUtc.Value)
                {
                    reachedCutoff = true;
                    continue;
                }

                if (message.Author.IsBot)
                {
                    continue;
                }

                if (!counts.TryGetValue(message.Author.Id, out var current))
                {
                    current = 0;
                }

                counts[message.Author.Id] = current + 1;
                total++;
            }

            beforeId = batch.Last().Id;
            delayState.Value = AdjustDelay(delayState.Value, fetchSw.ElapsedMilliseconds);
            await SafeProgressAsync(onProgress, new ProgressSnapshot(channel.Id, channel.Name, total, batch.Count, batches, delayState.Value, (int)fetchSw.ElapsedMilliseconds));
            await Task.Delay(delayState.Value);

            if (reachedCutoff)
            {
                break;
            }
        }

        return (total, counts);
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
        LastUpdateState lastUpdateUtc,
        DateTime? cutoffUtc)
    {
        var now = DateTime.UtcNow;
        if ((now - lastUpdateUtc.Value).TotalMilliseconds < 700)
        {
            return;
        }

        lastUpdateUtc.Value = now;
        var rateHint = progress.FetchMs >= 1500 ? " (possivel rate limit)" : string.Empty;
        var filterText = cutoffUtc.HasValue
            ? $"Filtro: **ultimos {(int)Math.Ceiling((DateTime.UtcNow - cutoffUtc.Value).TotalDays)} dias**\n"
            : string.Empty;

        var text =
            $"Recontagem em andamento...\n" +
            $"Canal: **#{progress.ChannelName}**\n" +
            $"Mensagens no canal: **{progress.ChannelTotal:N0}**\n" +
            $"Batch: **{progress.Batches}** | Ultimo lote: **{progress.BatchSize}**\n" +
            $"Delay atual: **{progress.DelayMs}ms** | Fetch: **{progress.FetchMs}ms**{rateHint}\n" +
            filterText +
            $"Canais lidos: **{channelsScanned}** | Pulados: **{channelsSkipped}**\n" +
            $"Total parcial: **{totalMessages:N0}**";

        try
        {
            await status.ModifyAsync(m => m.Content = text);
        }
        catch
        {
            
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

    private static async Task<int> CountUserMessagesAsync(
        SocketTextChannel channel,
        ulong userId,
        DelayState delayState,
        DateTime? cutoffUtc,
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
                delayState.Value = Math.Min(2000, delayState.Value + 300);
                await Task.Delay(delayState.Value);
                continue;
            }
            fetchSw.Stop();
            batches++;

            if (batch.Count == 0)
            {
                break;
            }

            var reachedCutoff = false;
            foreach (var message in batch)
            {
                if (cutoffUtc.HasValue && message.Timestamp.UtcDateTime < cutoffUtc.Value)
                {
                    reachedCutoff = true;
                    continue;
                }

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

            if (reachedCutoff)
            {
                break;
            }
        }

        return total;
    }

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

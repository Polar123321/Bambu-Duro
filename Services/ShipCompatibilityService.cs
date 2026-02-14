using Microsoft.EntityFrameworkCore;
using Discord.WebSocket;
using ConsoleApp4.Data;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class ShipCompatibilityService : IShipCompatibilityService
{
    private readonly BotDbContext _db;
    private readonly IUserHourStatsService _hours;

    public ShipCompatibilityService(BotDbContext db, IUserHourStatsService hours)
    {
        _db = db;
        _hours = hours;
    }

    public async Task<ShipCompatibilityResult> CalculateAsync(SocketGuild guild, ulong userId1, ulong userId2, CancellationToken cancellationToken = default)
    {
        
        var totals = await _db.UserGuildStats
            .AsNoTracking()
            .Where(s => s.DiscordGuildId == guild.Id && (s.DiscordUserId == userId1 || s.DiscordUserId == userId2))
            .Select(s => new { s.DiscordUserId, s.MessageCount, s.UpdatedAtUtc })
            .ToListAsync(cancellationToken);

        var t1 = totals.FirstOrDefault(x => x.DiscordUserId == userId1);
        var t2 = totals.FirstOrDefault(x => x.DiscordUserId == userId2);

        var m1 = Math.Max(0, t1?.MessageCount ?? 0);
        var m2 = Math.Max(0, t2?.MessageCount ?? 0);
        var u1 = t1?.UpdatedAtUtc;
        var u2 = t2?.UpdatedAtUtc;

        
        const int topN = 24;
        var c1 = await _db.UserChannelStats.AsNoTracking()
            .Where(s => s.DiscordGuildId == guild.Id && s.DiscordUserId == userId1 && s.MessageCount > 0)
            .OrderByDescending(s => s.MessageCount)
            .Take(topN)
            .Select(s => new { s.DiscordChannelId, s.MessageCount })
            .ToListAsync(cancellationToken);

        var c2 = await _db.UserChannelStats.AsNoTracking()
            .Where(s => s.DiscordGuildId == guild.Id && s.DiscordUserId == userId2 && s.MessageCount > 0)
            .OrderByDescending(s => s.MessageCount)
            .Take(topN)
            .Select(s => new { s.DiscordChannelId, s.MessageCount })
            .ToListAsync(cancellationToken);

        var vChan1 = c1.ToDictionary(x => x.DiscordChannelId, x => x.MessageCount);
        var vChan2 = c2.ToDictionary(x => x.DiscordChannelId, x => x.MessageCount);

        var channelSim = CosineSimilarity(vChan1, vChan2);
        var sharedChannels = vChan1.Keys.Intersect(vChan2.Keys).ToList();

        
        var vHour1 = await _hours.GetHourOfWeekCountsAsync(guild.Id, userId1);
        var vHour2 = await _hours.GetHourOfWeekCountsAsync(guild.Id, userId2);
        var hourSim = CosineSimilarity(vHour1, vHour2);

        var topSharedHours = TopSharedHours(vHour1, vHour2, k: 3);

        
        var balance = (m1 == 0 || m2 == 0) ? 0.35 : (double)Math.Min(m1, m2) / Math.Max(m1, m2);

        
        var recency = RecencyScore(u1, u2);

        
        var baseSpice = StableSpice(userId1, userId2);

        
        var score =
            (0.34 * channelSim) +
            (0.34 * hourSim) +
            (0.18 * recency) +
            (0.10 * balance) +
            (0.04 * baseSpice);

        
        var dataPoints = Math.Min(1.0, (Math.Log10(m1 + 10) + Math.Log10(m2 + 10)) / 6.0);
        score = (score * dataPoints) + ((0.55 * balance + 0.45 * baseSpice) * (1.0 - dataPoints));

        var percent = (int)Math.Clamp(Math.Round(score * 100.0), 0, 100);

        var title = TitleFor(percent);
        var summary = SummaryFor(percent, channelSim, hourSim);

        var channelsText = sharedChannels.Count == 0
            ? "Poucos canais em comum (ou dados insuficientes ainda)."
            : BuildSharedChannelsText(guild, sharedChannels);

        var hoursText = topSharedHours.Count == 0
            ? "Ainda sem padrao de horario em comum (precisa de mais mensagens)."
            : "Picos em comum: " + string.Join(", ", topSharedHours.Select(FormatHourOfWeek));

        var confidence = ConfidenceText(m1, m2, vHour1.Count, vHour2.Count);

        return new ShipCompatibilityResult(
            Percent: percent,
            Title: title,
            Summary: summary,
            SharedChannelsText: channelsText,
            ActiveHoursText: hoursText,
            ConfidenceText: confidence);
    }

    private static double CosineSimilarity<TKey>(IReadOnlyDictionary<TKey, int> a, IReadOnlyDictionary<TKey, int> b)
        where TKey : notnull
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0.0;
        }

        double dot = 0;
        double magA = 0;
        double magB = 0;

        foreach (var kv in a)
        {
            var x = (double)kv.Value;
            magA += x * x;
            if (b.TryGetValue(kv.Key, out var bv))
            {
                dot += x * bv;
            }
        }

        foreach (var kv in b)
        {
            var y = (double)kv.Value;
            magB += y * y;
        }

        if (magA <= 0 || magB <= 0)
        {
            return 0.0;
        }

        return Math.Clamp(dot / (Math.Sqrt(magA) * Math.Sqrt(magB)), 0.0, 1.0);
    }

    private static double RecencyScore(DateTime? u1, DateTime? u2)
    {
        if (u1 == null || u2 == null)
        {
            return 0.2;
        }

        var now = DateTime.UtcNow;
        var d1 = (now - u1.Value).TotalDays;
        var d2 = (now - u2.Value).TotalDays;
        var avg = (d1 + d2) / 2.0;

        
        var s = 1.0 / (1.0 + Math.Pow(avg / 7.0, 1.2));
        return Math.Clamp(s, 0.0, 1.0);
    }

    private static double StableSpice(ulong userId1, ulong userId2)
    {
        var id1 = Math.Min(userId1, userId2);
        var id2 = Math.Max(userId1, userId2);
        var seed = HashCode.Combine((long)id1, (long)id2, 0x5EED);
        var rng = new Random(seed);
        return rng.NextDouble();
    }

    private static List<int> TopSharedHours(IReadOnlyDictionary<int, int> a, IReadOnlyDictionary<int, int> b, int k)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return new List<int>();
        }

        
        var list = new List<(int Hour, long Score)>();
        foreach (var kv in a)
        {
            if (b.TryGetValue(kv.Key, out var bv))
            {
                var score = (long)kv.Value * bv;
                if (score > 0)
                {
                    list.Add((kv.Key, score));
                }
            }
        }

        return list
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Hour)
            .Take(k)
            .Select(x => x.Hour)
            .ToList();
    }

    private static string BuildSharedChannelsText(SocketGuild guild, List<ulong> channelIds)
    {
        var names = channelIds
            .Select(id => guild.GetChannel(id))
            .Where(ch => ch != null)
            .Select(ch => "#" + ch!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (names.Count == 0)
        {
            return $"{channelIds.Count} canais em comum (sem nomes disponiveis).";
        }

        return $"{channelIds.Count} canais em comum: {string.Join(", ", names)}";
    }

    private static string FormatHourOfWeek(int hourOfWeek)
    {
        hourOfWeek = Math.Clamp(hourOfWeek, 0, 167);
        var day = (DayOfWeek)(hourOfWeek / 24);
        var hour = hourOfWeek % 24;
        var dayPt = day switch
        {
            DayOfWeek.Sunday => "Dom",
            DayOfWeek.Monday => "Seg",
            DayOfWeek.Tuesday => "Ter",
            DayOfWeek.Wednesday => "Qua",
            DayOfWeek.Thursday => "Qui",
            DayOfWeek.Friday => "Sex",
            DayOfWeek.Saturday => "Sab",
            _ => day.ToString()
        };
        return $"{dayPt} {hour:00}h";
    }

    private static string TitleFor(int percent)
    {
        return percent switch
        {
            >= 90 => "ALMA GEMEA (ou bug no universo)",
            >= 75 => "Casal que faz barulho no chat",
            >= 55 => "Ship plausivel e perigoso",
            >= 35 => "Amizade com tensao dramatica",
            >= 15 => "Tem quimica... de laboratorio",
            _ => "So se for por contrato"
        };
    }

    private static string SummaryFor(int percent, double chan, double hour)
    {
        
        var chanTxt = chan >= 0.6 ? "muitos canais em comum" : chan >= 0.3 ? "alguns canais em comum" : "poucos canais em comum";
        var hourTxt = hour >= 0.6 ? "horarios bem alinhados" : hour >= 0.3 ? "horarios mais ou menos" : "horarios desencontrados";
        return $"{percent}%: {chanTxt} e {hourTxt}.";
    }

    private static string ConfidenceText(int m1, int m2, int hours1, int hours2)
    {
        var msg = m1 + m2;
        var hours = hours1 + hours2;
        if (msg >= 1500 && hours >= 50) return "Alta (tem bastante historico)";
        if (msg >= 400 && hours >= 20) return "Media (da pra confiar um pouco)";
        if (msg >= 80) return "Baixa (pouco historico ainda)";
        return "Muito baixa (quase sem dados, chute divertido)";
    }
}


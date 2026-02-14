using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.User;

public sealed class ProfileCommand : CommandBase
{
    private readonly IMarriageStore _marriageStore;

    public ProfileCommand(
        IMarriageStore marriageStore,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _marriageStore = marriageStore;
    }

    [Command("perfil")]
    [Alias("profile")]
    [Summary("Mostra informações do seu perfil no servidor.")]
    public async Task ProfileAsync()
    {
        await TrackUserAsync();

        if (Context.User is not SocketGuildUser user)
        {
            await ReplyAsync("Este comando só funciona em servidores.");
            return;
        }

        var spouseText = await ResolveSpouseTextAsync(user.Id);

        var embed = EmbedHelper.CreateInfo($"👤 Perfil de {user.Username}", $"{user.Mention}")
            .AddField("ID", user.Id, true)
            .AddField("Conta criada", user.CreatedAt.DateTime.ToString("dd/MM/yyyy"), true)
            .AddField("Entrou no servidor", user.JoinedAt?.DateTime.ToString("dd/MM/yyyy") ?? "Desconhecido", true)
            .AddField("Apelido", user.Nickname ?? "Nenhum", true)
            .AddField("Roles", Math.Max(user.Roles.Count - 1, 0), true)
            .AddField("Bot?", user.IsBot ? "Sim" : "Não", true)
            .AddField("Casamento", spouseText, true)
            .WithThumbnailUrl(user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl());

        await ReplyMajesticAsync(embed);
    }

    private async Task<string> ResolveSpouseTextAsync(ulong userId)
    {
        var records = await _marriageStore.GetAsync(0);
        var record = records.FirstOrDefault(r => r.UserId1 == userId || r.UserId2 == userId);
        if (record == null)
        {
            return "Solteiro(a)";
        }

        var spouseId = record.UserId1 == userId ? record.UserId2 : record.UserId1;
        var spouse = Context.Guild?.GetUser(spouseId) ?? Context.Client.GetUser(spouseId);
        if (spouse != null)
        {
            return spouse.Mention;
        }

        return $"Usuario {spouseId}";
    }
}

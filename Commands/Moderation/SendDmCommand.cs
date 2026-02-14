using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using System.Net.Http;

namespace ConsoleApp4.Commands.Moderation;

public sealed class SendDmCommand : CommandBase
{
    private const long MaxAttachmentBytes = 8L * 1024 * 1024; 
    private readonly IHttpClientFactory _httpClientFactory;

    public SendDmCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService,
        IHttpClientFactory httpClientFactory)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _httpClientFactory = httpClientFactory;
    }

    [Command("senddm")]
    [Summary("Envia uma mensagem via DM para um usuario (com suporte a anexos como imagens/videos).")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SendDmAsync(string target, [Remainder] string? message = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            await ReplyAsync("Informe o usuario.");
            return;
        }

        var attachments = Context.Message.Attachments ?? Array.Empty<Attachment>();
        var hasMessage = !string.IsNullOrWhiteSpace(message);
        var hasAttachments = attachments.Count > 0;

        if (!hasMessage && !hasAttachments)
        {
            await ReplyAsync("Informe a mensagem e/ou envie anexos junto com o comando.");
            return;
        }

        var user = await ResolveTargetAsync(Context.Guild, target);
        if (user == null)
        {
            await ReplyAsync("Nao encontrei o usuario informado neste servidor.");
            return;
        }

        try
        {
            var dm = await user.CreateDMChannelAsync();

            if (!hasAttachments)
            {
                await dm.SendMessageAsync(message!);
                await ReplyAsync($"DM enviada para {user.Mention}.");
                return;
            }

            var client = _httpClientFactory.CreateClient();
            var toDispose = new List<MemoryStream>();
            var files = new List<FileAttachment>();
            try
            {
                foreach (var a in attachments)
                {
                    
                    if (a.Size > MaxAttachmentBytes)
                    {
                        await ReplyAsync($"Anexo muito grande para enviar por DM: `{a.Filename}` ({a.Size / (1024 * 1024)}MB).");
                        continue;
                    }

                    if (!Uri.TryCreate(a.Url, UriKind.Absolute, out var uri) ||
                        !(uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                          uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)))
                    {
                        await ReplyAsync($"URL de anexo invalida: `{a.Filename}`.");
                        continue;
                    }

                    using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();

                    
                    await using var src = await resp.Content.ReadAsStreamAsync();
                    var ms = new MemoryStream(capacity: (int)Math.Min(a.Size, int.MaxValue));
                    await src.CopyToAsync(ms);
                    ms.Position = 0;

                    var safeName = SanitizeFileName(a.Filename);
                    toDispose.Add(ms);
                    files.Add(new FileAttachment(ms, safeName));
                }

                if (files.Count > 0)
                {
                    await dm.SendFilesAsync(files, text: hasMessage ? message : null);
                }
                else if (hasMessage)
                {
                    await dm.SendMessageAsync(message!);
                }

                await ReplyAsync($"DM enviada para {user.Mention}.");
            }
            finally
            {
                foreach (var s in toDispose)
                {
                    try { s.Dispose(); } catch { }
                }
            }
        }
        catch
        {
            await ReplyAsync("Nao consegui enviar a DM (o usuario pode ter bloqueado DMs).");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var n = string.IsNullOrWhiteSpace(name) ? "file" : name.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            n = n.Replace(c, '_');
        }
        return n;
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

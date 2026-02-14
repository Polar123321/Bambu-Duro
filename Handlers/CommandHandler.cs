using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;
using System.Text;
using System.Collections.Concurrent;

namespace ConsoleApp4.Handlers;

public sealed class CommandHandler
{
    private sealed record MentionMemoryData(string? PromptContext);

    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandHandler> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IGroqChatService _groq;
    private readonly IOptions<BotConfiguration> _config;
    private readonly IOptions<AutoNoticeConfiguration> _autoNotice;
    private readonly ConcurrentDictionary<ulong, IServiceScope> _commandScopes = new();
    private readonly SemaphoreSlim _mentionReplyConcurrency = new(initialCount: 4, maxCount: 4);

    public CommandHandler(
        DiscordSocketClient client,
        CommandService commands,
        IServiceProvider services,
        ILogger<CommandHandler> logger,
        IRateLimitService rateLimitService,
        IGroqChatService groq,
        IOptions<BotConfiguration> config,
        IOptions<AutoNoticeConfiguration> autoNotice)
    {
        _client = client;
        _commands = commands;
        _services = services;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _groq = groq;
        _config = config;
        _autoNotice = autoNotice;
    }

    public async Task InitializeAsync()
    {
        _client.MessageReceived += HandleMessageAsync;
        _commands.Log += OnLogAsync;
        _commands.CommandExecuted += OnCommandExecutedAsync;

        await _commands.AddModulesAsync(typeof(CommandHandler).Assembly, _services);
    }

    private async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot)
        {
            return;
        }

        try
        {
            RunDetached("AutoNotice", () => TryAutoNoticeAsync(message));

            var prefix = _config.Value.Prefix;
            var argPos = 0;
            if (!message.HasStringPrefix(prefix, ref argPos))
            {
                RunDetached("TrackAndMention", () => TryTrackAndMentionAsync(message));
                return;
            }

            RunDetached("TrackMessage", () => TryTrackMessageAsync(message));

            var context = new SocketCommandContext(_client, message);

            var commandToken = ExtractCommandToken(message.Content, argPos);
            _logger.LogInformation("Command debug: raw='{Raw}' token='{Token}' argPos={ArgPos} user={UserId}", message.Content, commandToken, argPos, message.Author.Id);

            // IMPORTANT: The CommandService is configured with RunMode.Async (it can execute after ExecuteAsync returns),
            // so we must keep the scope alive until CommandExecuted fires.
            var scope = _services.CreateScope();
            var scopedServices = scope.ServiceProvider;

            if (string.IsNullOrWhiteSpace(commandToken) || !IsKnownCommand(commandToken))
            {
                // Fallback: try to execute anyway in case of hidden chars or parsing quirks.
                var fallback = await _commands.ExecuteAsync(context, argPos, scopedServices);
                _logger.LogInformation("Command exec (fallback): success={Success} error={Error} reason='{Reason}' user={UserId}", fallback.IsSuccess, fallback.Error, fallback.ErrorReason, message.Author.Id);
                if (fallback.IsSuccess)
                {
                    TrackScopeForCommandExecution(message.Id, scope);
                }
                else
                {
                    scope.Dispose();
                    var localPrefix = _config.Value.Prefix;
                    var helpHint = string.IsNullOrWhiteSpace(localPrefix) ? "ajuda" : $"{localPrefix}ajuda";
                    try
                    {
                        await message.Channel.SendMessageAsync($"Comando desconhecido. Use {helpHint}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to send unknown-command hint. Channel={ChannelId} User={UserId}", message.Channel.Id, message.Author.Id);
                    }
                }
                return;
            }

            if (!_rateLimitService.TryConsume(message.Author.Id, "commands", TimeSpan.FromSeconds(2), out var retryAfter))
            {
                var wait = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                try
                {
                    await message.Channel.SendMessageAsync($"Voce esta enviando comandos rapido demais. Aguarde {wait}s.");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to send rate-limit message. Channel={ChannelId} User={UserId}", message.Channel.Id, message.Author.Id);
                }
                scope.Dispose();
                return;
            }

            var result = await _commands.ExecuteAsync(context, argPos, scopedServices);
            _logger.LogInformation("Command exec: success={Success} error={Error} reason='{Reason}' user={UserId}", result.IsSuccess, result.Error, result.ErrorReason, message.Author.Id);

            if (!result.IsSuccess)
            {
                scope.Dispose();
                _logger.LogWarning("Command error: {Error} - {Reason}", result.Error, result.ErrorReason);
                var response = BuildErrorMessage(context, result, message.Content, argPos);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    try
                    {
                        await message.Channel.SendMessageAsync(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to send command error message. Channel={ChannelId} User={UserId}", message.Channel.Id, message.Author.Id);
                    }
                }
            }
            else
            {
                TrackScopeForCommandExecution(message.Id, scope);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in message handler. Channel={ChannelId} User={UserId}", rawMessage.Channel.Id, rawMessage.Author.Id);
        }
    }

    private void RunDetached(string taskName, Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Background task failed: {TaskName}", taskName);
            }
        });
    }

    private void TrackScopeForCommandExecution(ulong messageId, IServiceScope scope)
    {
        _commandScopes[messageId] = scope;

        // Defensive cleanup in case CommandExecuted isn't raised for some reason.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(10)).ConfigureAwait(false);
            if (_commandScopes.TryRemove(messageId, out var stale))
            {
                stale.Dispose();
            }
        });
    }

    private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        // If the command ran under a per-message scope, dispose it now (RunMode.Async safe).
        if (context.Message != null && _commandScopes.TryRemove(context.Message.Id, out var scope))
        {
            scope.Dispose();
        }

        if (!command.IsSpecified)
        {
            return;
        }

        using var logScope = _services.CreateScope();
        var logger = logScope.ServiceProvider.GetRequiredService<ICommandLogService>();

        await logger.LogAsync(new CommandLog
        {
            Id = Guid.NewGuid(),
            UserId = context.User.Id,
            CommandName = command.Value.Name,
            GuildName = context.Guild?.Name,
            Success = result.IsSuccess,
            ErrorMessage = result.IsSuccess ? null : result.ErrorReason
        });
    }

    private Task OnLogAsync(LogMessage message)
    {
        // Discord.Net sometimes logs "Command: null" with no exception; keep noise down.
        if (message.Message != null &&
            message.Message.StartsWith("Command: null", StringComparison.OrdinalIgnoreCase) &&
            message.Exception == null)
        {
            return Task.CompletedTask;
        }

        if (message.Message == null && message.Exception == null)
        {
            return Task.CompletedTask;
        }

        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(level, message.Exception, "{Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    private string BuildErrorMessage(SocketCommandContext context, IResult result, string content, int argPos)
    {
        var prefix = _config.Value.Prefix;
        var helpHint = string.IsNullOrWhiteSpace(prefix) ? "ajuda" : $"{prefix}ajuda";

        if (result.Error == CommandError.UnknownCommand)
        {
            return $"Comando desconhecido. Use {helpHint}.";
        }

        if (result.Error == CommandError.UnmetPrecondition)
        {
            var reason = result.ErrorReason ?? "Permissao insuficiente.";
            if (reason.Contains("ManageGuild", StringComparison.OrdinalIgnoreCase))
            {
                return "Voce precisa da permissao 'Gerenciar Servidor' para usar este comando.";
            }

            if (reason.Contains("cooldown", StringComparison.OrdinalIgnoreCase))
            {
                return reason;
            }

            return $"Sem permissao: {reason}";
        }

        if (result.Error == CommandError.BadArgCount || result.Error == CommandError.ParseFailed)
        {
            var usage = TryBuildUsage(context, argPos);
            if (!string.IsNullOrWhiteSpace(usage))
            {
                return $"Uso correto: {usage}";
            }

            return $"Formato invalido. Use {helpHint} para exemplos.";
        }

        if (result.Error == CommandError.ObjectNotFound)
        {
            return "Nao encontrei o alvo/objeto informado. Verifique o valor e tente novamente.";
        }

        if (result.Error == CommandError.MultipleMatches)
        {
            return "Existem varios resultados possiveis. Seja mais especifico.";
        }

        if (result.Error == CommandError.Exception)
        {
            return "Ocorreu um erro interno ao executar o comando. Tente novamente em alguns segundos.";
        }

        return $"Nao consegui executar esse comando. Use {helpHint}.";
    }

    private string? TryBuildUsage(SocketCommandContext context, int argPos)
    {
        var search = _commands.Search(context, argPos);
        if (!search.IsSuccess || search.Commands.Count == 0)
        {
            return null;
        }

        var cmd = search.Commands[0].Command;
        var prefix = _config.Value.Prefix ?? "";
        var alias = cmd.Aliases.FirstOrDefault() ?? cmd.Name;

        var sb = new StringBuilder();
        sb.Append(prefix);
        sb.Append(alias);

        foreach (var p in cmd.Parameters)
        {
            sb.Append(' ');
            sb.Append(p.IsOptional ? $"[{p.Name}]" : $"<{p.Name}>");
        }

        return sb.ToString();
    }

    private bool IsKnownCommand(string commandToken)
    {
        return _commands.Commands.Any(c =>
            c.Aliases.Any(a => string.Equals(a, commandToken, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? ExtractCommandToken(string content, int argPos)
    {
        if (argPos >= content.Length)
        {
            return null;
        }

        var remainder = content[argPos..].TrimStart();
        if (remainder.Length == 0)
        {
            return null;
        }

        var token = remainder.Split(' ', '\t', '\r', '\n')[0];
        if (token.Length == 0)
        {
            return null;
        }

        return RemoveZeroWidth(token);
    }

    private static string RemoveZeroWidth(string input)
    {
        return input
            .Replace("\u200B", string.Empty)
            .Replace("\u200C", string.Empty)
            .Replace("\u200D", string.Empty)
            .Replace("\uFEFF", string.Empty);
    }

    private async Task TryAutoNoticeAsync(SocketUserMessage message)
    {
        var cfg = _autoNotice.Value;
        if (cfg.CooldownSeconds <= 0 || string.IsNullOrWhiteSpace(cfg.Message))
        {
            return;
        }

        if (message.Channel is not SocketGuildChannel guildChannel)
        {
            return;
        }

        if (message.Author is not SocketGuildUser guildUser)
        {
            return;
        }

        var isTargetByRole = cfg.RoleId != 0 && guildUser.Roles.Any(r => r.Id == cfg.RoleId);
        var isTargetByUser = cfg.UserIds != null && cfg.UserIds.Contains(guildUser.Id);
        if (!isTargetByRole && !isTargetByUser)
        {
            return;
        }

        // Bucket groups rate limiting per guild and config flavor; TryConsume is still per-author.
        var bucket = $"autonotice:{guildChannel.Guild.Id}:{cfg.RoleId}:{(cfg.UserIds != null && cfg.UserIds.Count > 0 ? 1 : 0)}";
        if (!_rateLimitService.TryConsume(guildUser.Id, bucket, TimeSpan.FromSeconds(cfg.CooldownSeconds)))
        {
            return;
        }

        try
        {
            await message.Channel.SendMessageAsync(
                text: cfg.Message.Trim(),
                messageReference: new MessageReference(
                    messageId: message.Id,
                    channelId: guildChannel.Id,
                    guildId: guildChannel.Guild.Id,
                    failIfNotExists: false),
                allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "AutoNotice failed. Guild={GuildId} Channel={ChannelId} User={UserId}", guildChannel.Guild.Id, guildChannel.Id, guildUser.Id);
        }
    }

    private async Task TryReplyToBotMentionAsync(SocketUserMessage message)
    {
        await _mentionReplyConcurrency.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_client.CurrentUser == null)
            {
                return;
            }

            var botId = _client.CurrentUser.Id;
            if (!message.MentionedUsers.Any(u => u.Id == botId))
            {
                return;
            }

            // Prevent spam when users keep pinging the bot.
            if (!_rateLimitService.TryConsume(message.Author.Id, "groq-mention", TimeSpan.FromSeconds(3)))
            {
                return;
            }

            var prompt = ExtractMentionPrompt(message.Content, botId);
            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = "oi";
            }

            string? guildName = (message.Channel as SocketGuildChannel)?.Guild?.Name;

            try
            {
                await message.Channel.TriggerTypingAsync();
                var memoryData = await TryBuildUserMemoryContextAsync(message, prompt);
                var answer = await _groq.MentionReplyAsync(prompt, message.Author.Username, guildName, memoryData?.PromptContext);
                if (answer.Length > 1800)
                {
                    answer = answer[..1800] + "...";
                }

                if (message.Channel is SocketGuildChannel guildChannel)
                {
                    await message.Channel.SendMessageAsync(
                        text: answer,
                        messageReference: new MessageReference(
                            messageId: message.Id,
                            channelId: guildChannel.Id,
                            guildId: guildChannel.Guild.Id,
                            failIfNotExists: false),
                        allowedMentions: AllowedMentions.None);
                }
                else
                {
                    await message.Channel.SendMessageAsync(text: answer, allowedMentions: AllowedMentions.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed Groq mention reply. Channel={ChannelId} User={UserId}", message.Channel.Id, message.Author.Id);
            }
        }
        finally
        {
            _mentionReplyConcurrency.Release();
        }
    }

    private async Task TryTrackAndMentionAsync(SocketUserMessage message)
    {
        await TryTrackMessageAsync(message).ConfigureAwait(false);
        await TryReplyToBotMentionAsync(message).ConfigureAwait(false);
    }

    private async Task<MentionMemoryData?> TryBuildUserMemoryContextAsync(SocketUserMessage message, string currentPrompt)
    {
        if (message.Channel is not SocketGuildChannel guildChannel)
        {
            return null;
        }

        try
        {
            using var scope = _services.CreateScope();
            var memory = scope.ServiceProvider.GetService<IUserMemoryService>();
            if (memory == null || !memory.ShouldTrackUser(message.Author.Id))
            {
                return null;
            }

            var context = await memory.GetContextAsync(
                guildChannel.Guild.Id,
                message.Author.Id,
                currentPrompt: currentPrompt,
                maxMessages: 16);
            var promptContext = memory.BuildPromptContext(context);
            return new MentionMemoryData(promptContext);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to build user memory context. Guild={GuildId} User={UserId}", guildChannel.Guild.Id, message.Author.Id);
            return null;
        }
    }

    private static string ExtractMentionPrompt(string content, ulong botId)
    {
        var text = RemoveZeroWidth(content ?? string.Empty);
        text = text.Replace($"<@{botId}>", " ", StringComparison.Ordinal);
        text = text.Replace($"<@!{botId}>", " ", StringComparison.Ordinal);
        text = text.Trim();
        if (text.Length > 1800)
        {
            text = text[..1800];
        }
        return text;
    }

    private async Task TryTrackMessageAsync(SocketUserMessage message)
    {
        if (message.Channel is not SocketGuildChannel guildChannel)
        {
            return;
        }

        try
        {
            using var scope = _services.CreateScope();
            var stats = scope.ServiceProvider.GetRequiredService<IUserGuildStatsService>();
            await stats.IncrementMessagesAsync(guildChannel.Guild.Id, guildChannel.Id, message.Author.Id);

            var hourStats = scope.ServiceProvider.GetService<IUserHourStatsService>();
            if (hourStats != null)
            {
                await hourStats.IncrementMessageAsync(guildChannel.Guild.Id, message.Author.Id, DateTime.UtcNow);
            }

            var memory = scope.ServiceProvider.GetService<IUserMemoryService>();
            if (memory != null)
            {
                await memory.CaptureMessageAsync(
                    guildId: guildChannel.Guild.Id,
                    channelId: guildChannel.Id,
                    userId: message.Author.Id,
                    username: message.Author.Username,
                    content: message.Content,
                    createdAtUtc: message.CreatedAt.UtcDateTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to track message for guild {GuildId}", guildChannel.Guild.Id);
        }
    }
}

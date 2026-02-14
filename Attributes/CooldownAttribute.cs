using Discord.Commands;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class CooldownAttribute : PreconditionAttribute
{
    private readonly int _seconds;

    public CooldownAttribute(int seconds)
    {
        _seconds = Math.Max(1, seconds);
    }

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        if (services.GetService(typeof(IRateLimitService)) is not IRateLimitService limiter)
        {
            return Task.FromResult(PreconditionResult.FromError("Rate limiter indisponivel."));
        }

        var bucket = $"cooldown:{command.Name}";
        if (!limiter.TryConsume(context.User.Id, bucket, TimeSpan.FromSeconds(_seconds), out var retryAfter))
        {
            var wait = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            return Task.FromResult(PreconditionResult.FromError($"Comando em cooldown. Aguarde {wait}s."));
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}

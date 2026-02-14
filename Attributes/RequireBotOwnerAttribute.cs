using Discord.Commands;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Attributes;

public sealed class RequireBotOwnerAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        var config = services.GetService(typeof(IOptions<BotConfiguration>)) as IOptions<BotConfiguration>;
        if (config == null || config.Value.OwnerUserId == 0)
        {
            return Task.FromResult(PreconditionResult.FromError("OwnerUserId não configurado."));
        }

        return context.User.Id == config.Value.OwnerUserId
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("Você não tem permissão para esse comando."));
    }
}
